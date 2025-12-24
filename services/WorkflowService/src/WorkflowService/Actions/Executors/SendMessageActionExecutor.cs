using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;
using WorkflowService.Interfaces;

namespace WorkflowService.Actions.Executors;

public class SendMessageActionExecutor(
    IMessageSender messageSender,
    ITopicProducer<ActionCompletedEvent> actionCompletedProducer
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Message;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("Sending message");
        await messageSender.SendAsync(action, message, ct);
        await actionCompletedProducer.Produce(new ActionCompletedEvent(message.Channel, message.ClientId), ct);
    }
}