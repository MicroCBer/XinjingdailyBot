using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using XinjingdailyBot.Infrastructure;
using XinjingdailyBot.Infrastructure.Attribute;
using XinjingdailyBot.Infrastructure.Enums;
using XinjingdailyBot.Infrastructure.Extensions;
using XinjingdailyBot.Interface.Bot.Common;
using XinjingdailyBot.Interface.Bot.Handler;
using XinjingdailyBot.Interface.Data;
using XinjingdailyBot.Interface.Helper;
using XinjingdailyBot.Model.Models;

namespace XinjingdailyBot.Command;

/// <summary>
/// 超级管理员命令
/// </summary>
[AppService(LifeTime.Scoped)]
internal class SuperCommand(
        ILogger<SuperCommand> _logger,
        ITelegramBotClient _botClient,
        IPostService _postService,
        IChannelOptionService _channelOptionService,
        IChannelService _channelService,
        IMarkupHelperService _markupHelperService,
        ICommandHandler _commandHandler,
        IUserService _userService,
        IHttpHelperService _httpHelperService,
        ITextHelperService _textHelperService,
        IAdvertiseService _advertiseService)
{
    /// <summary>
    /// 重启机器人
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("RESTART", EUserRights.SuperCmd, Description = "重启机器人")]
    public async Task ResponseRestart(Message message)
    {
        try
        {
            string path = Path.Exists(Environment.ProcessPath) ? Environment.ProcessPath : Utils.ExeFullPath;
            _logger.LogInformation("机器人运行路径: {path}", path);
            Process.Start(path);
            await _botClient.SendCommandReply("机器人即将重启", message);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await _botClient.SendCommandReply("启动进程遇到错误", message);
            _logger.LogError(ex, "启动进程遇到错误");
        }
    }

    /// <summary>
    /// 终止机器人
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("EXIT", EUserRights.SuperCmd, Description = "终止机器人")]
    public async Task ResponseExit(Message message)
    {
        await _botClient.SendCommandReply("机器人即将退出", message);
        Environment.Exit(0);
    }

    /// <summary>
    /// 来源频道设置
    /// </summary>
    /// <param name="dbUser"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("CHANNELOPTION", EUserRights.SuperCmd, Description = "来源频道设置")]
    public async Task ResponseChannalOption(Users dbUser, Message message)
    {
        async Task<(string, InlineKeyboardMarkup?)> exec()
        {
            if (message.Chat.Id != _channelService.ReviewGroup.Id)
            {
                return ("该命令仅限审核群内使用", null);
            }

            if (message.ReplyToMessage == null)
            {
                return ("请回复审核消息", null);
            }

            var post = await _postService.FetchPostFromReplyToMessage(message);

            if (post == null)
            {
                return ("未找到稿件", null);
            }

            if (!post.IsFromChannel)
            {
                return ("不是来自其他频道的投稿, 无法设置频道选项", null);
            }

            var channel = await _channelOptionService.FetchChannelByChannelId(post.ChannelID);

            if (channel == null)
            {
                return ("未找到对应频道", null);
            }

            string option = channel.Option switch {
                EChannelOption.Normal => "不做特殊处理",
                EChannelOption.PurgeOrigin => "抹除频道来源",
                EChannelOption.AutoReject => "拒绝此频道的投稿",
                _ => "未知的值",
            };

            var keyboard = _markupHelperService.SetChannelOptionKeyboard(dbUser, channel.ChannelID);

            return ($"请选择针对来自 {channel.ChannelTitle} 的稿件的处理方式\n当前设置: {option}", keyboard);
        }

        (var text, var kbd) = await exec();
        await _botClient.SendCommandReply(text, message, autoDelete: false, replyMarkup: kbd);
    }

    /// <summary>
    /// 来源频道设置
    /// </summary>
    /// <param name="query"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [QueryCmd("CHANNELOPTION", EUserRights.SuperCmd, Description = "来源频道设置")]
    public async Task QResponseChannalOption(CallbackQuery query, string[] args)
    {
        async Task<string> exec()
        {
            if (args.Length < 3)
            {
                return "参数有误";
            }

            if (!long.TryParse(args[1], out long channelId))
            {
                return "参数有误";
            }

            EChannelOption? option = args[2] switch {
                "normal" => EChannelOption.Normal,
                "purgeorigin" => EChannelOption.PurgeOrigin,
                "autoreject" => EChannelOption.AutoReject,
                _ => null
            };

            string optionStr = option switch {
                EChannelOption.Normal => "不做特殊处理",
                EChannelOption.PurgeOrigin => "抹除频道来源",
                EChannelOption.AutoReject => "拒绝此频道的投稿",
                _ => "未知的值",
            };

            if (option == null)
            {
                return $"未知的频道选项 {args[2]}";
            }

            var channel = await _channelOptionService.UpdateChannelOptionById(channelId, option.Value);

            if (channel == null)
            {
                return $"找不到频道 {channelId}";
            }

            return $"来自 {channel.ChannelTitle} 频道的稿件今后将被 {optionStr}";
        }

        string text = await exec();
        await _botClient.EditMessageTextAsync(query.Message!, text, replyMarkup: null);
    }

    /// <summary>
    /// 设置命令菜单
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("COMMAND", EUserRights.SuperCmd, Description = "设置命令菜单")]
    public async Task ResponseCommand(Message message)
    {
        bool result = await _commandHandler.SetCommandsMenu();
        await _botClient.SendCommandReply(result ? "设置菜单成功" : "设置菜单失败", message, autoDelete: false);
    }

    [TextCmd("CLEARCOMMAND", EUserRights.SuperCmd, Description = "设置命令菜单")]
    public async Task ResponseClearCommand(Message message)
    {
        bool result = await _commandHandler.ClearCommandsMenu();
        await _botClient.SendCommandReply(result ? "清除菜单成功" : "清除菜单失败", message, autoDelete: false);
    }

    /// <summary>
    /// 重新计算用户投稿数量
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("RECALCPOST", EUserRights.SuperCmd, Description = "重新计算用户投稿数量")]
    public async Task ResponseReCalcPost(Message message)
    {
        const int threads = 10;

        int startId = 1;
        int effectCount = 0;

        int totalUsers = await _userService.CountUser();

        var msg = await _botClient.SendCommandReply($"开始更新用户表, 共计 {totalUsers} 条记录", message, autoDelete: false);

        while (startId <= totalUsers)
        {
            var users = await _userService.GetUserListAfterId(startId, threads);
            if (users.Count == 0)
            {
                break;
            }

            var tasks = users.Select(async user => {
                int postCount = await _postService.Queryable().CountAsync(x => x.PosterUID == user.UserID);
                int acceptCount = await _postService.Queryable().CountAsync(x => x.PosterUID == user.UserID && (x.Status == EPostStatus.Accepted || x.Status == EPostStatus.AcceptedSecond));
                int rejectCount = await _postService.Queryable().CountAsync(x => x.PosterUID == user.UserID && x.Status == EPostStatus.Rejected);
                int expiredCount = await _postService.Queryable().CountAsync(x => x.PosterUID == user.UserID && x.Status < 0);
                int reviewCount = await _postService.Queryable().CountAsync(x => x.ReviewerUID == user.UserID && x.PosterUID != user.UserID);

                if (user.PostCount != postCount || user.AcceptCount != acceptCount || user.RejectCount != rejectCount || user.ExpiredPostCount != expiredCount || user.ReviewCount != reviewCount)
                {
                    user.PostCount = postCount;
                    user.AcceptCount = acceptCount;
                    user.RejectCount = rejectCount;
                    user.ExpiredPostCount = expiredCount;
                    user.ReviewCount = reviewCount;

                    effectCount++;

                    await _userService.UpdateUserPostCount(user);
                }
            }).ToList();

            await Task.WhenAll(tasks);

            startId += threads;

            _logger.LogInformation("更新进度 {startId} / {totalUsers}, 更新数量 {effectCount}", startId, totalUsers, effectCount);
        }

        try
        {
            await _botClient.EditMessageTextAsync(msg, $"更新用户表完成, 更新了 {effectCount} 条记录");
        }
        catch
        {
            await _botClient.SendCommandReply($"更新用户表完成, 更新了 {effectCount} 条记录", message, autoDelete: false);
        }
    }

    /// <summary>
    /// 自动升级机器人
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [TextCmd("UPDATE", EUserRights.SuperCmd, Description = "自动升级机器人")]
    public async Task ResponseUpdate(Message message)
    {
        async Task<string> exec()
        {
            if (!BuildInfo.CanUpdate)
            {
                return "当前版本不支持自动升级";
            }

            var releaseResponse = await _httpHelperService.GetLatestRelease();

            if (releaseResponse == null)
            {
                return "读取在线版本信息失败";
            }

            if (Utils.Version == releaseResponse.TagName)
            {
                return string.Format("当前已经是最新版本 {0}", Utils.Version);
            }

            string varint = BuildInfo.Variant;
            string? downloadUrl = null;

            foreach (var asset in releaseResponse.Assets)
            {
                if (asset.Name.Contains(varint))
                {
                    downloadUrl = asset.DownloadUrl;
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl) && releaseResponse.Assets.Count != 0)
            {
                return "自动更新失败, 找不到适配的更新包";
            }

            var bs = await _httpHelperService.DownloadRelease(downloadUrl).ConfigureAwait(false);

            if (bs == null)
            {
                return "自动更新失败, 下载更新包失败";
            }

            try
            {
                await using (bs.ConfigureAwait(false))
                {
                    using var zipArchive = new ZipArchive(bs);

                    string currentPath = Utils.ExeFullPath;
                    string backupPath = Utils.BackupFullPath;

                    System.IO.File.Move(currentPath, backupPath, true);

                    int count = 0;
                    foreach (var entry in zipArchive.Entries)
                    {
                        if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                            || entry.FullName.Equals(Utils.ExeFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ExtractToFile(currentPath);
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine(string.Format("自动更新成功, 更新了{0}个文件", count));
                        sb.AppendLine(string.Format("版本变动: {0} -> {1}", Utils.Version, releaseResponse.TagName));
                        sb.AppendLine();
                        sb.AppendLine("发行版日志:");
                        sb.AppendLine(string.Format("<code>{0}</code>", releaseResponse.Body));
                        sb.AppendLine();
                        sb.AppendLine(_textHelperService.HtmlLink(releaseResponse.Url, "在线查看"));
                        return sb.ToString();
                    }
                    else
                    {
                        System.IO.File.Move(backupPath, currentPath);
                        return "自动更新失败, 无文件变动";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动更新失败, 解压遇到问题");
                return "自动更新失败, 解压遇到问题";
            }
        }

        string text = await exec();
        await _botClient.SendCommandReply(text, message, false, ParseMode.Html);
    }

    /// <summary>
    /// 新建广告
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [TextCmd("CREATEAD", EUserRights.SuperCmd, Alias = "NEWAD", Description = "新建广告")]
    public async Task ResponseCreateAd(Message message)
    {
        var replyMsg = message.ReplyToMessage;

        if (replyMsg == null)
        {
            await _botClient.SendCommandReply("请回复广告消息", message, false);
            return;
        }

        await _advertiseService.CreateAdvertise(replyMsg);

        await _botClient.SendCommandReply("创建广告成功, 请在数据库中修改广告配置并启用该广告", message, false, parsemode: ParseMode.Html);
    }
}
