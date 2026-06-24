namespace WorkflowService.Models.Node;

public interface IHasRecipient
{
    bool SendToCustomRecipient { get; set; }
    Guid? CustomRecipientClientChannelId { get; set; }
}
