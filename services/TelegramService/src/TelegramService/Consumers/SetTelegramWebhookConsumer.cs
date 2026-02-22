using MassTransit;
using Shared.Application.Events;
using TelegramService.Interfaces;

namespace TelegramService.Consumers;

public sealed class SetTelegramWebhookConsumer(
    ITelegramClient telegramClient,
    ILogger<SetTelegramWebhookConsumer> logger)
    : IConsumer<TelegramSetWebhookEvent>
{
    public async Task Consume(ConsumeContext<TelegramSetWebhookEvent> context)
    {
        var token = context.Message.Token;

        logger.LogInformation(
            "Setting telegram token: {token}",
            token);

        await telegramClient.SetWebhookAsync(context.Message.ChannelId, token,
            context.CancellationToken);
    }
}