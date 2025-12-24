using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;
using WorkflowService.Interfaces;

namespace WorkflowService.Actions.Executors;

public class AskActionExecutor(
    IMessageSender messageSender
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Ask;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("AskActionExecutor");
        await messageSender.SendAsync(action, message, ct);
    }
}