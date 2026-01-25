namespace WorkflowService.Models.Node;


public sealed class AskNodeData : MessageNodeData
{
    public AskUiData? Ui { get; init; }
}