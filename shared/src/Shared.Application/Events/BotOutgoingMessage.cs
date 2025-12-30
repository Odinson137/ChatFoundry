using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record BotOutgoingMessage(
    DefaultChannel Channel,
    string ExternalUserId,
    string MessageJson,
    MessageKind MessageKind);