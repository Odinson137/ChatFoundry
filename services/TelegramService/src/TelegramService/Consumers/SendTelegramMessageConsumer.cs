using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Shared.Domain.Models;
using TelegramService.Interfaces;
using Newtonsoft.Json;

namespace TelegramService.Consumers;

public sealed class SendTelegramMessageConsumer(
    ITelegramClient telegramClient,
    ITopicProducer<ActionCompletedEvent> producer,
    ILogger<SendTelegramMessageConsumer> logger)
    : IConsumer<BotOutgoingMessage>
{
    public async Task Consume(ConsumeContext<BotOutgoingMessage> context)
    {
        var message = context.Message;
        if (message.Channel != DefaultChannel.Telegram) return;
        
        if (string.IsNullOrEmpty(message.MessageJson)) return;

        logger.LogInformation(
            "Sending telegram message to {ChatId} kind={MessageKind}", 
            message.ExternalUserId, message.MessageKind);

        await DispatchByMessageKind(message.MessageKind, message.MessageJson, message.ExternalUserId, context.CancellationToken);
    }

    private async Task DispatchByMessageKind(MessageKind kind, string messageJson, string chatId, CancellationToken ct)
    {
        switch (kind)
        {
            case MessageKind.Text:
            case MessageKind.Link:
                var textPayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendTextAsync(chatId, textPayload.Text, ct);
                break;

            case MessageKind.Buttons:
                var buttonsPayload = JsonConvert.DeserializeObject<AskMessagePayload>(messageJson)!;
                await telegramClient.SendInlineKeyboardAsync(
                    chatId, 
                    buttonsPayload.Text, 
                    buttonsPayload.Buttons, 
                    ct);
                break;

            case MessageKind.File:
                var filePayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendDocumentAsync(chatId, filePayload.Text, filePayload.Caption, ct);
                break;

            case MessageKind.Image:
                var imagePayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendPhotoAsync(chatId, imagePayload.Text, imagePayload.Caption, ct);
                break;

            case MessageKind.Video:
                var videoPayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendVideoAsync(chatId, videoPayload.Text, videoPayload.Caption, ct);
                break;

            case MessageKind.Audio:
            case MessageKind.Voice:
            case MessageKind.Sticker:
                // пока так
                var fallbackText = JsonConvert.DeserializeObject<MessagePayload>(messageJson)?.Text ?? "Unsupported media";
                await telegramClient.SendTextAsync(chatId, fallbackText, ct);
                break;

            default:
                logger.LogWarning("Unknown message kind: {MessageKind}", kind);
                break;
        }
    }
}
