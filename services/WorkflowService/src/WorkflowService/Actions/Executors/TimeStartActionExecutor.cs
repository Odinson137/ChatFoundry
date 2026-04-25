using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;

namespace WorkflowService.Actions.Executors;

public class TimeStartActionExecutor(ITopicProducer<ActionCompletedEvent> actionCompletedProducer
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.TimerStart;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("Start timer message");
        await actionCompletedProducer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}