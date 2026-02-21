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
    [HttpPost("{BotId:guid}")]
    public async Task<IActionResult> ReceivedMessage([FromRoute] Guid botId, [FromBody] TelegramUpdateDto body,
        CancellationToken token)
    {
        if (body.UpdateId == 0 || botId == Guid.Empty)
        {
            logger.LogError($"Invalid update: {JsonConvert.SerializeObject(body)} for bot {botId}");
            return Ok();
        }

        var messageEvent = ProcessUpdate(botId, body);
        if (messageEvent != null)
        {
            await producer.Produce(messageEvent, token);
        }

        return Ok();
    }

    private BotIncomingMessage? ProcessUpdate(Guid botId, TelegramUpdateDto update)
    {
        if (update.CallbackQuery != null)
        {
            return new BotIncomingMessage(
                botId,
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
            ? CreateTextMessage(botId, message, chatId, messageId)
            : message switch
            {
                { Photo.Count: > 0 } => CreatePhotoMessage(botId, message, chatId, messageId),
                { Sticker: not null } => CreateStickerMessage(botId, message, chatId, messageId),
                { Document: not null } => CreateDocumentMessage(botId, message, chatId, messageId),
                { Voice: not null } => CreateVoiceMessage(botId, message, chatId, messageId),
                _ => null
            };
    }

    private BotIncomingMessage CreateTextMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Text!, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            }
        );
    }

    private BotIncomingMessage CreatePhotoMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        // берём самую качественную фотку
        var largePhoto = message.Photo!.OrderBy(c => c.FileSize).Last();

        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, largePhoto.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private BotIncomingMessage CreateStickerMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Sticker!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private BotIncomingMessage CreateDocumentMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Document!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }

    private BotIncomingMessage CreateVoiceMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Voice!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Media
        );
    }
}