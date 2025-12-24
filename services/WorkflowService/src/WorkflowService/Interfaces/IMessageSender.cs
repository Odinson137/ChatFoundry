using WorkflowService.Entities;
using WorkflowService.Events;

namespace WorkflowService.Interfaces;

public interface IMessageSender
{
    Task SendAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct);
}