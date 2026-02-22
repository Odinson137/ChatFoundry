using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record BotOutgoingMessage(
    Guid ChannelId,
    DefaultChannel Channel,
    string ExternalUserId,
    string MessageJson,
    MessageKind MessageKind,
    Guid? CompanyId = null);