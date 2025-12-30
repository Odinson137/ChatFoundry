using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IWorkflowRepository
{
    Task<BotWorkflow?> GetActiveWorkflowAsync(Guid botId, CancellationToken ct);
    Task<BotWorkflow?> GetByIdAsync(Guid id);
    Task SaveAsync(BotWorkflow workflow);
    Task<BotWorkflow?> GetActionWorkflowAsync(Guid actionId);
}