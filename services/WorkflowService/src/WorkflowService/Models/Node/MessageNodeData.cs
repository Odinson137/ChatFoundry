namespace WorkflowService.Models.Node;

public class MessageNodeData : NodeData, IHasRecipient
{
    public string Text { get; init; } = null!;
    public bool SendToCustomRecipient { get; set; }
    public Guid? CustomRecipientClientChannelId { get; set; }
}