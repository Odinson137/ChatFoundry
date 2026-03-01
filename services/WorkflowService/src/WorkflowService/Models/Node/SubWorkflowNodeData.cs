namespace WorkflowService.Models.Node;

public sealed class SubWorkflowNodeData : NodeData
{
    public Guid WorkflowId { get; init; }

    /// <summary>
    /// Child parameter name -> expression with {{parentVar}} templates.
    /// Example: { "user_query": "{{lastMessage}}", "lang": "en" }
    /// </summary>
    public Dictionary<string, string> InputMappings { get; init; } = new();

    /// <summary>
    /// Parent variable name -> child variable name.
    /// Example: { "result": "ai_response", "score": "confidence" }
    /// </summary>
    public Dictionary<string, string> OutputMappings { get; init; } = new();
}