namespace Shared.Application.Events;

public record BotIncomingMessage(Guid BotId, string ClientId, string Channel, string Payload);