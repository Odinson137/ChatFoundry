using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class SetVariableActionExecutor(
    ITopicProducer<ActionCompletedEvent> producer,
    ISessionRepository sessionRepository,
    WorkflowGraphParser workflowGraphParser)
    : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.SetVariable;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("SetVariableActionExecutor");

        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not SetVariableNodeData setVariableData)
            return;

        var renderedValue = WorkflowTextRenderer.RenderText(setVariableData.Value, session);

        session.SetVariable(setVariableData.Variable, renderedValue);
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
