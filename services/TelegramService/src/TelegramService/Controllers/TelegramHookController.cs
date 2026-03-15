using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Enums;
using TelegramService.Interfaces;
using TelegramService.Models;

namespace TelegramService.Controllers;

[ApiController]
[Route("hook")]
public class TelegramHookController(
    ITopicProducer<BotIncomingMessage> producer,
    ILogger<TelegramHookController> logger,
    IMediaUploader mediaUploader)
    : ControllerBase
{
    [HttpPost("{ChannelId:guid}")]
    public async Task<IActionResult> ReceivedMessage([FromRoute] Guid channelId, [FromBody] TelegramUpdateDto body,
        CancellationToken token)
    {
        if (body.UpdateId == 0 || channelId == Guid.Empty)
        {
            logger.LogError("Invalid update: {Body} for channel {ChannelId}", JsonConvert.SerializeObject(body), channelId);
            return Ok();
        }

        var messageEvent = await ProcessUpdateAsync(channelId, body, token);
        if (messageEvent != null)
        {
            await producer.Produce(messageEvent, token);
        }

        return Ok();
    }

    private async Task<BotIncomingMessage?> ProcessUpdateAsync(Guid channelId, TelegramUpdateDto update, CancellationToken ct)
    {
        if (update.CallbackQuery != null)
        {
            return new BotIncomingMessage(
                channelId,
                update.CallbackQuery.From.Id.ToString(),
                DefaultChannel.Telegram,
                update.CallbackQuery.Data ?? "",
                update.CallbackQuery.Id,
                new Dictionary<MessageParameter, string>
                {
                    [MessageParameter.FirstName] = update.CallbackQuery.From.FirstName,
                    [MessageParameter.UserName] = update.CallbackQuery.From.Username ?? ""
                },
                MessageKind.CallbackQuery
            );
        }

        var message = update.Message ?? update.EditedMessage;
        if (message == null) return null;

        var chatId = message.Chat.Id.ToString();
        var messageId = message.MessageId.ToString();

        if (message.Text != null)
            return CreateTextMessage(channelId, message, chatId, messageId);

        var (fileId, fileName, mimeType) = message switch
        {
            { Photo.Count: > 0 } => (message.Photo!.OrderBy(c => c.FileSize).Last().FileId, (string?)null, "image/jpeg"),
            { Sticker: not null } => (message.Sticker!.FileId, (string?)null, "image/webp"),
            { Document: not null } => (message.Document!.FileId, message.Document.FileName, message.Document.MimeType),
            { Voice: not null } => (message.Voice!.FileId, (string?)null, message.Voice.MimeType ?? "audio/ogg"),
            { Video: not null } => (message.Video!.FileId, message.Video.FileName, message.Video.MimeType ?? "video/mp4"),
            { Audio: not null } => (message.Audio!.FileId, message.Audio.FileName, message.Audio.MimeType ?? "audio/mpeg"),
            _ => (null, null, null)
        };

        if (fileId == null)
            return null;

        var payload = await UploadAndBuildPayloadAsync(channelId, fileId, fileName, mimeType, message.Caption, ct);

        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, payload, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private async Task<string> UploadAndBuildPayloadAsync(
        Guid channelId, string fileId, string? fileName, string? mimeType, string? caption, CancellationToken ct)
    {
        var result = await mediaUploader.DownloadAndUploadAsync(channelId, fileId, fileName, mimeType, ct);

        return result switch
        {
            MediaUploadSuccess success => JsonSerialize(new { text = success.FileId.ToString(), caption }),
            MediaUploadSizeExceeded => JsonSerialize(new { error = "size_exceeded", telegram_file_id = fileId, caption }),
            MediaUploadFailed => JsonSerialize(new { error = "upload_failed", telegram_file_id = fileId, caption }),
            _ => JsonSerialize(new { error = "upload_failed", telegram_file_id = fileId, caption })
        };
    }

    private static string JsonSerialize(object value) =>
        System.Text.Json.JsonSerializer.Serialize(value, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    private static BotIncomingMessage CreateTextMessage(Guid channelId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, message.Text!, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            }
        );
    }
}
