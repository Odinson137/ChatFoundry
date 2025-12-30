namespace Shared.Application.Events;

public sealed record TelegramSetWebhookEvent(Guid BotId, string Url);
