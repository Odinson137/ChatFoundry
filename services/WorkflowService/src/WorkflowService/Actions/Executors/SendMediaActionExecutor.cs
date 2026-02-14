using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;

namespace WorkflowService.Actions.Executors;

public class SendMediaActionExecutor(
    IMessageSender messageSender,
    ITopicProducer<ActionCompletedEvent> actionCompletedProducer
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Media;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        await messageSender.SendAsync(action, message, ct);
        
        // TODO перенести только после отправки файла
        await actionCompletedProducer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
