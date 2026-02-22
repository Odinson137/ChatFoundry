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
    ITelegramClient telegramClient)
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

        var messageEvent = ProcessUpdate(channelId, body);
        if (messageEvent != null)
        {
            await producer.Produce(messageEvent, token);
        }

        return Ok();
    }

    private BotIncomingMessage? ProcessUpdate(Guid channelId, TelegramUpdateDto update)
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

        return message.Text != null
            ? CreateTextMessage(channelId, message, chatId, messageId)
            : message switch
            {
                { Photo.Count: > 0 } => CreatePhotoMessage(channelId, message, chatId, messageId),
                { Sticker: not null } => CreateStickerMessage(channelId, message, chatId, messageId),
                { Document: not null } => CreateDocumentMessage(channelId, message, chatId, messageId),
                { Voice: not null } => CreateVoiceMessage(channelId, message, chatId, messageId),
                _ => null
            };
    }

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

    private static BotIncomingMessage CreatePhotoMessage(Guid channelId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        var largePhoto = message.Photo!.OrderBy(c => c.FileSize).Last();
        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, largePhoto.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private static BotIncomingMessage CreateStickerMessage(Guid channelId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, message.Sticker!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private static BotIncomingMessage CreateDocumentMessage(Guid channelId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, message.Document!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private static BotIncomingMessage CreateVoiceMessage(Guid channelId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            channelId, chatId, DefaultChannel.Telegram, message.Voice!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }
}