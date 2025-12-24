namespace Shared.Application.Events;

public sealed class TelegramSetWebhookEvent
{
    public string Url { get; init; } = null!;
}