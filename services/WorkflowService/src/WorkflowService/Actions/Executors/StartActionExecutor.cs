using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;

namespace WorkflowService.Actions.Executors;

public class StartActionExecutor(ITopicProducer<ActionCompletedEvent> producer) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Start;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("StartActionExecutor");
        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}