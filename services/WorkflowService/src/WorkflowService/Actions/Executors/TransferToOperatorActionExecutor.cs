using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;

namespace WorkflowService.Actions.Executors;

public class TransferToOperatorActionExecutor(
    IMessageSender messageSender,
    ISessionRepository sessionRepository,
    ITopicProducer<LiveChatRequestedEvent> producer
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.TransferToOperator;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        await messageSender.SendAsync(action, message, ct);
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            throw new InvalidOperationException($"Session {action.SessionId} not found");

        var workflow = session.Workflow;

        await producer.Produce(new LiveChatRequestedEvent(
            session.Id,
            session.ClientId,
            session.Channel,
            session.ChannelId,
            workflow.BotId,
            workflow.Bot.Name,
            workflow.Bot.CompanyId,
            null,
            null,
            "Transferring to operator..."
        ), ct);
    }
}
