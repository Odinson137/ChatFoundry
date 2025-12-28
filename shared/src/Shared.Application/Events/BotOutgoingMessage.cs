using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record BotOutgoingMessage(DefaultChannel Channel, string ExternalUserId, string Message, MessageKind MessageKind);
