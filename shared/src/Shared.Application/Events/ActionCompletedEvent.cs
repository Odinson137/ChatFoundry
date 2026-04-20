using Shared.Domain.Enums;

namespace Shared.Application.Events;

public record ActionCompletedEvent(
    DefaultChannel Channel,
    string ClientId,
    Guid? CompanyId = null,
    bool CountAsAiWorkflowExecution = false,
    bool Success = true);
