namespace WorkflowService.Models.Node;

public sealed class WaitNodeData : NodeData
{
    public string Duration { get; init; } = "60";
    public string Unit { get; init; } = "Seconds";
}
