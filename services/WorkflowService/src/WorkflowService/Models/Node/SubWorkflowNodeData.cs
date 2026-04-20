namespace WorkflowService.Models.Node;

public sealed class SubWorkflowNodeData : NodeData
{
    public Guid WorkflowId { get; init; }

    public Dictionary<string, string> InputMappings { get; init; } = new();

    public Dictionary<string, string> OutputMappings { get; init; } = new();
}