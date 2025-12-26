using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record BotIncomingMessage(
    Guid BotId, 
    string ExternalUserId, 
    DefaultChannels Channel, 
    string Payload,
    string MessageExternalId,
    IReadOnlyDictionary<MessageParameter, string> Parameters,
    MessageKind MessageKind = MessageKind.Text
    );