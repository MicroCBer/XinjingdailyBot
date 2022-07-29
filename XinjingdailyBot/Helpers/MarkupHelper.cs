﻿using Telegram.Bot.Types.ReplyMarkups;
using XinjingdailyBot.Enums;
using XinjingdailyBot.Localization;

namespace XinjingdailyBot.Helpers
{
    internal sealed class MarkupHelper
    {
        private static readonly string AnymouseOn = Emojis.Ghost + "匿名投稿";
        private static readonly string AnymouseOff = Emojis.Thinking + "保留来源";
        private static readonly string PostCancel = Emojis.No + "取消";
        private static readonly string PostConfirm = Emojis.Yes + "投稿";

        private static readonly string TagNSFWOn = "#NSFW";
        private static readonly string TagFriendOn = "#我有一个朋友";
        private static readonly string TagWanAnOn = "#晚安";

        private static readonly string TagNSFWOff = "#N___";
        private static readonly string TagFriendOff = "#我_____";
        private static readonly string TagWanAnOff = "#晚_";

        private static readonly string ReviewReject = Emojis.No + "拒绝";
        private static readonly string ReviewAccept = Emojis.Yes + "采用";

        private static readonly string RejectFuzzy = "模糊";
        private static readonly string RejectDuplicate = "重复";
        private static readonly string RejectBoring = "无趣";
        private static readonly string RejectConfusing = "没懂";
        private static readonly string RejectQRCode = "二维码";
        private static readonly string RejectDeny = "内容不合适";
        private static readonly string RejectOther = "其他原因";

        private static readonly string RejectCancel = Emojis.Back + "返回";

        internal static InlineKeyboardMarkup PostKeyboard(bool anymouse)
        {
            InlineKeyboardMarkup keyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData( anymouse ? AnymouseOn : AnymouseOff, "post anymouse"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData( PostCancel,  "post cancel"),
                    InlineKeyboardButton.WithCallbackData( PostConfirm,  "post confirm"),
                },
            });
            return keyboard;
        }

        internal static InlineKeyboardMarkup DirectPostKeyboard(bool anymouse, BuildInTags tag)
        {
            InlineKeyboardMarkup keyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData( tag.HasFlag(BuildInTags.NSFW)? TagNSFWOn:TagNSFWOff, "review tag nsfw"),
                    InlineKeyboardButton.WithCallbackData(tag.HasFlag(BuildInTags.WanAn)? TagWanAnOn:TagWanAnOff, "review tag wanan"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(tag.HasFlag(BuildInTags.Friend)? TagFriendOn:TagFriendOff, "review tag friend"),
                },

                new []
                {
                    InlineKeyboardButton.WithCallbackData( anymouse ? AnymouseOn : AnymouseOff, "review anymouse"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData( PostCancel,  "review cancel"),
                    InlineKeyboardButton.WithCallbackData( ReviewAccept,  "review accept"),
                },
            });
            return keyboard;
        }

        internal static InlineKeyboardMarkup ReviewKeyboardA(BuildInTags tag)
        {
            InlineKeyboardMarkup keyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData( tag.HasFlag(BuildInTags.NSFW)? TagNSFWOn:TagNSFWOff, "review tag nsfw"),
                    InlineKeyboardButton.WithCallbackData(tag.HasFlag(BuildInTags.WanAn)? TagWanAnOn:TagWanAnOff, "review tag wanan"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(tag.HasFlag(BuildInTags.Friend)? TagFriendOn:TagFriendOff, "review tag friend"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData( ReviewReject,  "review reject"),
                    InlineKeyboardButton.WithCallbackData( ReviewAccept,  "review accept"),
                },
            });
            return keyboard;
        }

        internal static InlineKeyboardMarkup ReviewKeyboardB()
        {
            InlineKeyboardMarkup keyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(RejectFuzzy, "reject fuzzy"),
                    InlineKeyboardButton.WithCallbackData(RejectDuplicate, "reject duplicate"),
                    InlineKeyboardButton.WithCallbackData(RejectBoring, "reject boring"),
                    InlineKeyboardButton.WithCallbackData(RejectConfusing, "reject confusing"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(RejectDeny, "reject deny"),
                    InlineKeyboardButton.WithCallbackData(RejectQRCode, "reject qrcode"),
                    InlineKeyboardButton.WithCallbackData(RejectOther, "reject other"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData( RejectCancel,  "reject back"),
                },
            });
            return keyboard;
        }

        internal static InlineKeyboardMarkup SetUserGroupKeyboard()
        {
            InlineKeyboardMarkup keyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("普通用户", "usergroup user"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("审核员", "setusergroup reviewer"),
                    InlineKeyboardButton.WithCallbackData("发布员", "setgroup poster"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("狗管理", "setgroup admin"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("超级狗管理", "setgroup super"),
                },
            });
            return keyboard;
        }
    }
}
