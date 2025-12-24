namespace Shared.Application.Events;

public record ActionCompletedEvent(string Channel, string ClientId);