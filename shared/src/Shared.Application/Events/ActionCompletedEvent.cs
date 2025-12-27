using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record ActionCompletedEvent(DefaultChannels Channel, string ClientId);