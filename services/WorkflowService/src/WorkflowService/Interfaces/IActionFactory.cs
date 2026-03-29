using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Interfaces;

public interface IActionFactory
{
    Task<ActionEntity> CreateAsync(Session session,
        WorkflowNode workflowGraph, WorkflowNodeType workflowNodeType,
        string? payload = null, MessageKind messageKind = MessageKind.Unknown,
        CancellationToken cancellationToken = default);
}