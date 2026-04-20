namespace WorkflowService.Models.Node;

public class AIGenerateNodeData : NodeData, IContinueOnError
{
    public string Prompt { get; set; } = string.Empty;

    public bool IncludeChatContext { get; set; }

    public bool ContinueOnError { get; set; }
}
