using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Interfaces;

public interface IActionFactory
{
    Task<ActionEntity> CreateAsync(Session session,
        WorkflowNode workflowGraph, WorkflowNodeType workflowNodeType,
        string? payload = null, CancellationToken cancellationToken = default);
}