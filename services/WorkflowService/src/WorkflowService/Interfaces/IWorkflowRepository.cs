using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IWorkflowRepository
{
    Task<Workflow?> GetActiveWorkflowAsync(Guid botId, CancellationToken ct);
    Task<Workflow?> GetByIdAsync(Guid id);
    Task SaveAsync(Workflow workflow);
    Task<Workflow?> GetActionWorkflowAsync(Guid actionId);
}