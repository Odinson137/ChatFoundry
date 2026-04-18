namespace WorkflowService.Models.Node;

public class AIGenerateNodeData : NodeData, IContinueOnError
{
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Учитывать контекст чата текущей сессии (сообщения клиента и бота в этом диалоге).
    /// </summary>
    public bool IncludeChatContext { get; set; }

    /// <summary>
    /// При ошибке AI-запроса продолжать выполнение воркфлоу к следующей ноде.
    /// Если false — сессия завершается при ошибке.
    /// </summary>
    public bool ContinueOnError { get; set; }
}
