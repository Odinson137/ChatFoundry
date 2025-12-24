using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Constants;
using TelegramService.Interfaces;

namespace TelegramService.Consumers;

public sealed class SendTelegramMessageConsumer(
    ITelegramClient telegramClient,
    ITopicProducer<ActionCompletedEvent> producer,
    ILogger<SendTelegramMessageConsumer> logger)
    : IConsumer<TelegramSendMessageEvent>
{
    public async Task Consume(ConsumeContext<TelegramSendMessageEvent> context)
    {
        var message = context.Message;
        if (message == null || message.ChatId == null || message.Text == null) return;

        logger.LogInformation(
            "Sending telegram message to {ChatId}",
            message.ChatId);

        await telegramClient.SendTextAsync(
            message.ChatId,
            message.Text,
            context.CancellationToken);
        
    }
}