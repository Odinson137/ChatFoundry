using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class InputExecutor(ITopicProducer<ActionCompletedEvent> producer, ISessionRepository sessionRepository, WorkflowGraphParser workflowGraphParser) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Input;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        Console.WriteLine("InputExecutor");

        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null) 
            return;
        
        var graph = workflowGraphParser.Parse(session.Workflow.SchemaJson);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        var variable = (node.Data as AskNodeData)?.Variable;
        if (!string.IsNullOrWhiteSpace(variable))
        {
            session.SetVariable(variable, action.Payload);
            await sessionRepository.SaveAsync(session, ct);
        }
        
        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ClientId), ct);
    }
}