namespace WorkflowService.Models.Node;

public sealed class SubWorkflowNodeData : NodeData
{
    public Guid WorkflowId { get; init; }
}