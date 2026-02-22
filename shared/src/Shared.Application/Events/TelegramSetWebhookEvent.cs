namespace Shared.Application.Events;

public sealed record TelegramSetWebhookEvent(Guid ChannelId, string Token);
