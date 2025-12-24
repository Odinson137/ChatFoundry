namespace WorkflowService.Models.Node;

public sealed class AskUiData
{
    public IReadOnlyList<AskButton> Buttons { get; init; } = [];
}

public sealed class AskButton
{
    public string Value { get; init; } = null!;
    public string Text { get; init; } = null!;
}