using Shared.Application.Events;

namespace WorkflowService.Interfaces;

public interface IWorkflowOrchestrator
{
    Task HandleIncomingMessage(BotIncomingMessage msg, CancellationToken ct);
    Task OnActionCompleted(string msgChannelId, string actionId, CancellationToken ct);
    Task OnActionFailed(Guid actionId, CancellationToken ct);
}