using Shared.Domain.Enums;

namespace Shared.Application.Events;

/// <param name="CompanyId">Set when the workflow session's bot has a company; used for billing usage.</param>
/// <param name="CountAsAiWorkflowExecution">True when an AI model was invoked (e.g. AIGenerate block).</param>
public record ActionCompletedEvent(
    DefaultChannel Channel,
    string ClientId,
    Guid? CompanyId = null,
    bool CountAsAiWorkflowExecution = false);