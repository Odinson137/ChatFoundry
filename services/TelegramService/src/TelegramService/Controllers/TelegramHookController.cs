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

        var messageEvent = await ProcessUpdate(botId, body, token);
        if (messageEvent != null)
        {
            await producer.Produce(messageEvent, token);
        }

        return Ok();
    }

    private async Task<BotIncomingMessage?> ProcessUpdate(Guid botId, TelegramUpdateDto update, CancellationToken token)
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
                { Photo.Count: > 0 } => await CreatePhotoMessage(botId, message, chatId, messageId, token),
                { Sticker: not null } => await CreateStickerMessage(botId, message, chatId, messageId, token),
                { Document: not null } => await CreateDocumentMessage(botId, message, chatId, messageId, token),
                { Voice: not null } => await CreateVoiceMessage(botId, message, chatId, messageId, token),
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

    private async Task<BotIncomingMessage> CreatePhotoMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId, CancellationToken token)
    {
        // берём самую качественную фотку
        var largePhoto = message.Photo!.OrderBy(c => c.FileSize).Last();
        // var url = await telegramClient.GetFileAsync(largePhoto.FileId, token);

        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, largePhoto.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Image
        );
    }

    private async Task<BotIncomingMessage> CreateStickerMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId, CancellationToken token)
    {
        // var fileUrl = await GetFileUrlAsync(message.Sticker!.FileId, Token, token);

        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Sticker!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Sticker
        );
    }

    private async Task<BotIncomingMessage> CreateDocumentMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId, CancellationToken token)
    {
        //var fileUrl = await GetFileUrlAsync(message.Document!.FileId, Token, token);

        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Document!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.File
        );
    }

    private async Task<BotIncomingMessage> CreateVoiceMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId, CancellationToken token)
    {
        //var fileUrl = await GetFileUrlAsync(message.Voice!.FileId, Token, token);

        return new BotIncomingMessage(
            botId, chatId, DefaultChannel.Telegram, message.Voice!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = message.From?.FirstName ?? "",
                [MessageParameter.UserName] = message.From?.Username ?? ""
            },
            MessageKind.Voice
        );
    }
}