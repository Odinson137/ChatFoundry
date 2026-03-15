namespace WorkflowService.Models.Node;

public class AIGenerateNodeData : NodeData
{
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Учитывать контекст чата текущей сессии (сообщения клиента и бота в этом диалоге).
    /// </summary>
    public bool IncludeChatContext { get; set; }
}
