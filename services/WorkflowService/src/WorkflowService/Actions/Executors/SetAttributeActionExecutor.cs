using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class SetAttributeActionExecutor(
    ITopicProducer<ActionCompletedEvent> producer,
    ISessionRepository sessionRepository,
    IVariableService variableService,
    WorkflowGraphParser workflowGraphParser,
    WorkflowTextRenderer workflowTextRenderer)
    : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.SetAttribute;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not SetAttributeNodeData setAttributeData)
            return;

        var renderedValue = workflowTextRenderer.RenderText(setAttributeData.Value, session);

        variableService.SetAttribute(session, setAttributeData.Attribute, renderedValue);
        await variableService.SyncIfDirtyAsync(session, ct);
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
