namespace WorkflowService.Models.Node;


public sealed class AskNodeData : MessageNodeData
{
    /// <summary>
    /// Имя переменной, куда будет сохранён ответ
    /// </summary>
    public string? Variable { get; init; }

    public AskUiData? Ui { get; init; }
}