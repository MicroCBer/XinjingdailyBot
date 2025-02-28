using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using XinjingdailyBot.Infrastructure.Attribute;
using XinjingdailyBot.Infrastructure.Enums;
using XinjingdailyBot.Infrastructure.Extensions;
using XinjingdailyBot.Infrastructure.Localization;
using XinjingdailyBot.Interface.Bot.Common;
using XinjingdailyBot.Interface.Data;
using XinjingdailyBot.Interface.Helper;
using XinjingdailyBot.Model.Models;
using XinjingdailyBot.Repository;

namespace XinjingdailyBot.Command;

/// <summary>
/// 审核命令
/// </summary>
[AppService(LifeTime.Scoped)]
internal class ReviewCommand(
        ITelegramBotClient _botClient,
        IUserService _userService,
        IChannelService _channelService,
        IPostService _postService,
        IMarkupHelperService _markupHelperService,
        RejectReasonRepository _rejectReasonRepository,
        ITextHelperService _textHelperService)
{

    /// <summary>
    /// 自定义拒绝稿件理由
    /// </summary>
    /// <param name="dbUser"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [TextCmd("NO", EUserRights.ReviewPost, Description = "自定义拒绝稿件理由")]
    public async Task ResponseNo(Users dbUser, Message message, string[] args)
    {
        async Task<string> exec()
        {
            if (message.Chat.Id != _channelService.ReviewGroup.Id)
            {
                return "该命令仅限审核群内使用";
            }

            if (message.ReplyToMessage == null)
            {
                return "请回复审核消息并输入拒绝理由";
            }

            var post = await _postService.FetchPostFromReplyToMessage(message);
            if (post == null)
            {
                return "未找到稿件";
            }

            if (post.Status != EPostStatus.Reviewing)
            {
                return "仅能编辑状态为审核中的稿件";
            }

            var reason = string.Join(' ', args).Trim();

            if (string.IsNullOrEmpty(reason))
            {
                return "请输入拒绝理由";
            }

            var htmlText = _textHelperService.ParseMessage(message)[4..];
            post.RejectReason = reason;
            var rejectReason = new RejectReasons {
                Name = reason,
                FullText = reason,
            };
            await _postService.RejectPost(post, dbUser, rejectReason, htmlText);

            return $"已拒绝该稿件, 理由: {htmlText}";
        }

        var text = await exec();
        await _botClient.SendCommandReply(text, message, false, ParseMode.Html);
    }

    /// <summary>
    /// 修改稿件文字说明
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [TextCmd("EDIT", EUserRights.ReviewPost, Description = "修改稿件文字说明")]
    public async Task ResponseEditPost(Message message, string[] args)
    {
        async Task<string> exec()
        {
            if (message.Chat.Type != ChatType.Private && message.Chat.Id != _channelService.ReviewGroup.Id)
            {
                return "该命令仅限审核群内使用";
            }

            if (message.ReplyToMessage == null)
            {
                return "请回复审核消息并输入需要替换的描述";
            }

            var post = await _postService.FetchPostFromReplyToMessage(message);
            if (post == null)
            {
                return "未找到稿件";
            }

            if (post.Status != EPostStatus.Reviewing)
            {
                return "仅能编辑状态为审核中的稿件";
            }

            var postUser = await _userService.FetchUserByUserID(post.PosterUID);
            if (postUser == null)
            {
                return "未找到投稿用户";
            }

            var text = string.Join(' ', args).Trim();
            await _postService.EditPostText(post, text);

            return "稿件描述已更新";
        }

        var text = await exec();
        await _botClient.SendCommandReply(text, message, false);
    }

    /// <summary>
    /// 处理稿件
    /// </summary>
    /// <param name="dbUser"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [QueryCmd("REVIEW", EUserRights.ReviewPost, Alias = "REJECT", Description = "审核稿件")]
    public async Task HandleQuery(Users dbUser, CallbackQuery query)
    {
        var message = query.Message!;
        var post = await _postService.FetchPostFromCallbackQuery(query);
        if (post == null)
        {
            await _botClient.AutoReplyAsync("未找到稿件", query, true);
            await _botClient.EditMessageReplyMarkupAsync(message, null);
            return;
        }

        if (post.Status == EPostStatus.ReviewTimeout || post.Status == EPostStatus.ConfirmTimeout)
        {
            var msg = "该稿件已过期, 无法操作";
            await _botClient.AutoReplyAsync(msg, query);
            await _botClient.EditMessageTextAsync(message, msg, null);
            return;
        }

        if (post.Status != EPostStatus.Reviewing)
        {
            await _botClient.AutoReplyAsync("请不要重复操作", query, true);
            await _botClient.EditMessageReplyMarkupAsync(message, null);
            return;
        }

        if (!dbUser.Right.HasFlag(EUserRights.ReviewPost))
        {
            await _botClient.AutoReplyAsync("无权操作", query, true);
            return;
        }

        var data = query.Data;
        if (string.IsNullOrEmpty(data))
        {
            await _botClient.AutoReplyAsync("内部错误", query, true);
            return;
        }

        switch (data)
        {
            case "review reject":
                await SwitchKeyboard(true, post, query);
                break;

            //兼容旧的callback data
            case "reject back":
            case "review reject back":
                await SwitchKeyboard(false, post, query);
                break;

            case "review spoiler":
                await SetSpoiler(post, query);
                break;

            case "review inplan":
                await _postService.AcceptPost(post, dbUser, true, false, query);
                break;

            case "review accept":
                await _postService.AcceptPost(post, dbUser, false, false, query);
                break;

            case "review accept second":
                await _postService.AcceptPost(post, dbUser, false, true, query);
                break;

            case "review anymouse":
                await SetAnonymous(post, query);
                break;

            case "review cancel":
                await CancelPost(post, query);
                break;

            default:
                if (data.StartsWith("review tag"))
                {
                    var payload = data[11..];
                    if (payload != "spoiler")
                    {
                        await _postService.SetPostTag(post, payload, query);
                    }
                    else
                    {
                        await SetSpoiler(post, query);
                    }
                }
                else if (data.StartsWith("reject "))
                {
                    var payload = data[7..];
                    await RejectPostHelper(post, dbUser, query, payload);
                }
                break;
        }

    }

    /// <summary>
    /// 设置或者取消匿名
    /// </summary>
    /// <param name="post"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task SetAnonymous(NewPosts post, CallbackQuery query)
    {
        await _botClient.AutoReplyAsync("可以使用命令 /anonymous 切换默认匿名投稿", query);

        bool anonymous = !post.Anonymous;
        await _postService.SetPostAnonymous(post, anonymous);

        bool? hasSpoiler = post.CanSpoiler ? post.HasSpoiler : null;

        var keyboard = _markupHelperService.DirectPostKeyboard(anonymous, post.Tags, hasSpoiler);
        await _botClient.EditMessageReplyMarkupAsync(query.Message!, keyboard);
    }

    /// <summary>
    /// 设置或者取消遮罩
    /// </summary>
    /// <param name="post"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task SetSpoiler(NewPosts post, CallbackQuery query)
    {
        if (!post.CanSpoiler)
        {
            await _botClient.AutoReplyAsync("当前稿件类型无法设置遮罩", query, true);
            return;
        }

        var hasSpoiler = !post.HasSpoiler;

        await _postService.SetPostSpoiler(post, hasSpoiler);

        await _botClient.AutoReplyAsync(hasSpoiler ? "启用遮罩" : "禁用遮罩", query);

        var keyboard = post.IsDirectPost ?
            _markupHelperService.DirectPostKeyboard(post.Anonymous, post.Tags, hasSpoiler) :
            _markupHelperService.ReviewKeyboardA(post.Tags, hasSpoiler);
        await _botClient.EditMessageReplyMarkupAsync(query.Message!, keyboard);
    }

    /// <summary>
    /// 取消投稿
    /// </summary>
    /// <param name="post"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task CancelPost(NewPosts post, CallbackQuery query)
    {
        await _postService.CancelPost(post);

        await _botClient.EditMessageTextAsync(query.Message!, Langs.PostCanceled, replyMarkup: null);

        await _botClient.AutoReplyAsync(Langs.PostCanceled, query);
    }

    /// <summary>
    /// 拒绝稿件包装方法
    /// </summary>
    /// <param name="post"></param>
    /// <param name="dbUser"></param>
    /// <param name="query"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    private async Task RejectPostHelper(NewPosts post, Users dbUser, CallbackQuery query, string payload)
    {
        var reason = _rejectReasonRepository.GetReasonByPayload(payload);
        if (reason == null)
        {
            await _botClient.AutoReplyAsync($"找不到 {payload} 对应的拒绝理由", query, true);
            return;
        }
        await _postService.RejectPost(post, dbUser, reason, null);
    }

    /// <summary>
    /// 设置inlineKeyboard
    /// </summary>
    /// <param name="rejectMode"></param>
    /// <param name="post"></param>
    /// <param name="callbackQuery"></param>
    /// <returns></returns>
    private async Task SwitchKeyboard(bool rejectMode, NewPosts post, CallbackQuery callbackQuery)
    {
        if (rejectMode)
        {
            await _botClient.AutoReplyAsync("请选择拒稿原因", callbackQuery);
        }

        bool? hasSpoiler = post.CanSpoiler ? post.HasSpoiler : null;

        var keyboard = rejectMode ?
            _markupHelperService.ReviewKeyboardB() :
            _markupHelperService.ReviewKeyboardA(post.Tags, hasSpoiler);

        await _botClient.EditMessageReplyMarkupAsync(callbackQuery.Message!, keyboard);
    }
}
