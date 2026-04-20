using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record LiveChatRequestedEvent(
    Guid SessionId,
    string ExternalUserId,
    DefaultChannel Channel,
    Guid ChannelId,
    Guid BotId,
    string? BotName,
    Guid? CompanyId,
    string? ClientFirstName,
    string? ClientUserName,
    string? LastMessagePreview
);
