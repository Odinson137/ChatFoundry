namespace WorkflowService.Models.Node;

public sealed class SetAttributeNodeData : NodeData
{
    public string Attribute { get; init; } = null!;
    public string Value { get; init; } = null!;
}
