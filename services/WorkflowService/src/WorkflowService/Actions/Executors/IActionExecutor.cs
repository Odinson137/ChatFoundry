using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;

namespace WorkflowService.Actions.Executors;

public interface IActionExecutor
{
    WorkflowNodeType WorkflowNodeType { get; }

    Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct);
}