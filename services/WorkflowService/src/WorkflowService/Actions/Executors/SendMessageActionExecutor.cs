using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
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
        // todo пока так, потом может другое придумать
        await actionCompletedProducer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}