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

        await DispatchByMessageKind(message.ChannelId, message.MessageKind, message.MessageJson, message.ExternalUserId, context.CancellationToken);
    }

    private async Task DispatchByMessageKind(Guid channelId, MessageKind kind, string messageJson, string chatId, CancellationToken ct)
    {
        switch (kind)
        {
            case MessageKind.Text:
            case MessageKind.Link:
                var textPayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendTextAsync(channelId, chatId, textPayload.Text, ct);
                break;

            case MessageKind.Buttons:
                var buttonsPayload = JsonConvert.DeserializeObject<AskMessagePayload>(messageJson)!;
                await telegramClient.SendInlineKeyboardAsync(channelId, chatId, buttonsPayload.Text, buttonsPayload.Buttons, ct);
                break;

            case MessageKind.Photo:
            case MessageKind.Video:
            case MessageKind.Audio:
            case MessageKind.Voice:
            case MessageKind.Document:
            case MessageKind.Sticker:
                var mediaPayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson)!;
                await telegramClient.SendMediaAsync(channelId, chatId, mediaPayload.Text, mediaPayload.Caption, ct);
                break;

            default:
                logger.LogWarning("Unknown message kind: {MessageKind}", kind);
                break;
        }
    }
}
