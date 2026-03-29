using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Interfaces;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Actions.Factories;

public class ActionFactory : IActionFactory
{
    public Task<ActionEntity> CreateAsync(Session session,
        WorkflowNode workflowGraph, WorkflowNodeType workflowNodeType,
        string? payload = null, MessageKind messageKind = MessageKind.Unknown,
        CancellationToken cancellationToken = default)
    {
        var action = new ActionEntity
        {
            SessionId = session.Id,
            NodeId = session.CurrentNodeId!.Value,
            WorkflowNodeType = workflowNodeType,
            Status = ActionStatus.Pending,
            Payload = payload,
            MessageKind = messageKind
        };

        return Task.FromResult(action);
    }
}