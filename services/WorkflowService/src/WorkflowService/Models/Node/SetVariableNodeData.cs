namespace WorkflowService.Models.Node;

public sealed class SetVariableNodeData : NodeData
{
    public string Variable { get; init; } = null!;

    public string Value { get; init; } = null!;
}
