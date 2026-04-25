using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record BotIncomingMessage(
    Guid ChannelId,
    string ExternalUserId,
    DefaultChannel Channel,
    string Payload,
    string MessageExternalId,
    IReadOnlyDictionary<MessageParameter, string> Parameters,
    MessageKind MessageKind = MessageKind.Text,
    Guid? CompanyId = null,
    BotIncomingMessageSource Source = BotIncomingMessageSource.Client
);
