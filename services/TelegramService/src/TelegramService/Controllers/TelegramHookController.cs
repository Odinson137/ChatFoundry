using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Enums;
using TelegramService.Models;

namespace TelegramService.Controllers;

[ApiController]
[Route("telegramhook")]
public class TelegramHookController(ITopicProducer<BotIncomingMessage> producer, ILogger<TelegramHookController> logger)
    : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok();
    }

    [HttpPost("{BotId:guid}")]
    public async Task<IActionResult> ReceivedMessage([FromRoute] Guid botId, [FromBody] TelegramUpdateDto body,
        CancellationToken token)
    {
        if (body.UpdateId == 0 || botId == Guid.Empty)
        {
            logger.LogError($"Invalid update: {JsonConvert.SerializeObject(body)} for bot {botId}");
            return Ok();
        }

        var messageEvent = ProcessUpdate(botId, body, token);
        if (messageEvent != null)
        {
            //await producer.Produce(messageEvent, token);
        }

        return Ok();
    }

    private BotIncomingMessage? ProcessUpdate(Guid botId, TelegramUpdateDto update, CancellationToken token)
    {
        if (update.CallbackQuery != null)
        {
            return new BotIncomingMessage(
                botId,
                update.CallbackQuery.From.Id.ToString(),
                DefaultChannels.Telegram,
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

        return message.Text != null ? CreateTextMessage(botId, message, chatId, messageId) :
            message.Photo?.Any() == true ? CreatePhotoMessage(botId, message, chatId, messageId) :
            message.Sticker != null ? CreateStickerMessage(botId, message, chatId, messageId) :
            null;
    }

    private BotIncomingMessage CreateTextMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannels.Telegram, message.Text!, messageId,
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
        var largestPhoto = message.Photo!.Last(); // TODO отправлять все фотки разом
        return new BotIncomingMessage(
            botId, chatId, DefaultChannels.Telegram, largestPhoto.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                //[MessageParameter.FileId] = largestPhoto.FileId,
                //[MessageParameter.FileSize] = largestPhoto.FileSize?.ToString() ?? "",
                [MessageParameter.FirstName] = message.From?.FirstName ?? ""
            },
            MessageKind.Photo
        );
    }

    private BotIncomingMessage CreateStickerMessage(Guid botId, TelegramMessageDto message, string chatId,
        string messageId)
    {
        return new BotIncomingMessage(
            botId, chatId, DefaultChannels.Telegram, message.Sticker!.FileId, messageId,
            new Dictionary<MessageParameter, string>
            {
                //[MessageParameter.FileId] = message.Sticker.FileId,
                //[MessageParameter.Emoji] = message.Sticker.Emoji ?? "",
                [MessageParameter.FirstName] = message.From?.FirstName ?? ""
            },
            MessageKind.Sticker
        );
    }
}