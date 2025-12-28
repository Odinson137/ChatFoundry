using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using TelegramService.Interfaces;

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
        
        if (string.IsNullOrEmpty(message.Message)) return;

        logger.LogInformation(
            "Sending telegram message to {ChatId}",
            message.ExternalUserId);

        await telegramClient.SendTextAsync(
            message.ExternalUserId,
            message.Message,
            context.CancellationToken);
        
    }
}