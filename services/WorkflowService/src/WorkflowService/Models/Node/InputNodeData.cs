namespace WorkflowService.Models.Node;

public sealed class InputNodeData : NodeData
{
    public string InputType { get; init; } = null!;
    public IReadOnlyList<InputOption> Options { get; init; }
        = [];
}

public sealed class InputOption
{
    public string Id { get; init; } = null!;
    public string Text { get; init; } = null!;
}