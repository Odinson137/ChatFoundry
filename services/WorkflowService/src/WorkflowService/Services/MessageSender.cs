using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Constants;
using WorkflowService.Entities;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class MessageSender(
    ITopicProducer<TelegramSendMessageEvent> producer,
    WorkflowGraphParser workflowGraphParser,
    IWorkflowRepository workflowRepository,
    ISessionRepository sessionRepository,
    WorkflowTextRenderer workflowTextRenderer
) : IMessageSender
{
    public async Task SendAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var workflow = await workflowRepository.GetActionWorkflowAsync(action.Id)
                       ?? throw new Exception("Workflow not found");

        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        var graph = workflowGraphParser.Parse(workflow.SchemaJson);
        var node = graph.GetNode(session!.CurrentNodeId!.Value);

        var text = workflowTextRenderer.RenderNodeText(node, action.Session);

        await producer.Produce(
            new TelegramSendMessageEvent
            {
                ChatId = message.ClientId,
                Text = text
            },
            ct
        );
    }
}
