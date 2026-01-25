namespace WorkflowService.Models.Node;

public class MessageNodeData : NodeData
{
    public string Text { get; init; } = null!;
    
    /// <summary>
    /// Имя переменной, куда будет сохранён результат (ID сообщения, ответ пользователя и т.д.)
    /// </summary>
    public string? Variable { get; init; }
}