using SqlSugar;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using XinjingdailyBot.Infrastructure.Attribute;
using XinjingdailyBot.Interface.Data;
using XinjingdailyBot.Model.Models;
using XinjingdailyBot.Service.Data.Base;

namespace XinjingdailyBot.Service.Data;

/// <inheritdoc cref="IAttachmentService"/>
[AppService(typeof(IAttachmentService), LifeTime.Transient)]
internal sealed class AttachmentService(ISqlSugarClient context) : BaseService<Attachments>(context), IAttachmentService
{
    public Attachments? GenerateAttachment(Message message, long postID)
    {
        string? fileID, fileName, fileUid, mimeType;
        long size;
        int height, width;

        switch (message.Type)
        {
            case MessageType.Photo:
                {
                    var x = message.Photo!.Last();
                    fileID = x.FileId;
                    fileName = "";
                    fileUid = x.FileUniqueId;
                    mimeType = "";
                    size = x.FileSize ?? 0;
                    height = x.Height;
                    width = x.Width;
                }
                break;
            case MessageType.Audio:
                {
                    var x = message.Audio!;
                    fileID = x.FileId;
                    fileName = x.Title ?? x.FileName ?? "";
                    fileUid = x.FileUniqueId;
                    mimeType = x.MimeType ?? "";
                    size = x.FileSize ?? 0;
                    height = -1;
                    width = -1;
                }
                break;

            case MessageType.Video:
                {
                    var x = message.Video!;
                    fileID = x.FileId;
                    fileName = x.FileName ?? "";
                    fileUid = x.FileUniqueId;
                    mimeType = x.MimeType ?? "";
                    size = x.FileSize ?? 0;
                    height = x.Height;
                    width = x.Width;
                }
                break;
            case MessageType.Voice:
                {
                    var x = message.Voice!;
                    fileID = x.FileId;
                    fileName = "";
                    fileUid = x.FileUniqueId;
                    mimeType = "";
                    size = x.FileSize ?? 0;
                    height = -1;
                    width = -1;
                }
                break;
            case MessageType.Document:
                {
                    var x = message.Document!;
                    fileID = x.FileId;
                    fileName = x.FileName ?? "";
                    fileUid = x.FileUniqueId;
                    mimeType = x.MimeType ?? "";
                    size = x.FileSize ?? 0;
                    height = -1;
                    width = -1;
                }
                break;
            case MessageType.Animation:
                {
                    var x = message.Animation!;
                    fileID = x.FileId;
                    fileName = x.FileName ?? "";
                    fileUid = x.FileUniqueId;
                    mimeType = x.MimeType ?? "";
                    size = x.FileSize ?? 0;
                    height = x.Height;
                    width = x.Width;
                }
                break;
            default:
                return null;
        }

        var result = new Attachments {
            PostID = postID,
            FileID = fileID,
            FileName = fileName,
            FileUniqueID = fileUid,
            MimeType = mimeType,
            Size = size,
            Height = height,
            Width = width,
            Type = message.Type,
        };

        return result;
    }

    public Task CreateAttachment(Attachments attachment)
    {
        return Insertable(attachment).ExecuteCommandAsync();
    }

    public Task CreateAttachments(List<Attachments> attachments)
    {
        return Storageable(attachments).ExecuteCommandAsync();
    }

    public Task<Attachments> FetchAttachmentByPostId(long postId)
    {
        return Queryable().FirstAsync(x => x.PostID == postId);
    }

    public Task<List<Attachments>> FetchAttachmentsByPostId(long postId)
    {
        return Queryable().Where(x => x.PostID == postId).ToListAsync();
    }

    public Task<int> GetAttachmentCount()
    {
        return Queryable().CountAsync();
    }
}
