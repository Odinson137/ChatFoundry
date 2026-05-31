namespace WorkflowService.Models.Node;

public sealed class WebhookWaitNodeData : NodeData
{
    public int TimeoutSeconds { get; init; }

    public string CallbackUrlTemplate { get; init; } = string.Empty;
}
