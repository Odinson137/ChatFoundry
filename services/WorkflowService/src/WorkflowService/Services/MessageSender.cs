using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class MessageSender(
    ITopicProducer<BotOutgoingMessage> producer,
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
        var graph = workflowGraphParser.Parse(workflow.NodesDefinition, workflow.EdgesDefinition);
        var node = graph.GetNode(session!.CurrentNodeId!.Value);

        var messageKind = MessageKindMapper.FromNodeType(node.Type);
        var text = WorkflowTextRenderer.RenderNodeText(node, action.Session, messageKind);
        
        await producer.Produce(
            new BotOutgoingMessage(DefaultChannel.Telegram, message.ExternalUserId, text, messageKind),
            ct
        );
    }
}