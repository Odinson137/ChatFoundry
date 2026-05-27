using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record ActionCompletedEvent(
    DefaultChannel Channel,
    string ClientId,
    Guid? CompanyId = null,
    int AiTokensUsed = 0,
    bool Success = true);
