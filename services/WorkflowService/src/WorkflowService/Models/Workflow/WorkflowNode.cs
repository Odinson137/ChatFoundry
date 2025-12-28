using Shared.Domain.Enums;
using WorkflowService.Enums;
using WorkflowService.Models.Node;

namespace WorkflowService.Models.Workflow;

public sealed class WorkflowNode
{
    public Guid Id { get; init; }
    public WorkflowNodeType Type { get; init; }
    public string Label { get; init; } = null!;
    public NodeData Data { get; init; } = NodeData.Empty;
}