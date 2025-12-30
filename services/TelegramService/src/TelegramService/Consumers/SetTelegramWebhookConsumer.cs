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
        var webhook = context.Message.Url;

        logger.LogInformation(
            "Setting telegram webhook: {Url}",
            webhook);

        await telegramClient.SetWebhookAsync(context.Message.BotId, webhook,
            context.CancellationToken);
    }
}