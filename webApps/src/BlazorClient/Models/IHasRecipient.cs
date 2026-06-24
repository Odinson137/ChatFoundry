namespace BlazorClient.Models;

public interface IHasRecipient
{
    bool SendToCustomRecipient { get; set; }
    Guid? CustomRecipientClientChannelId { get; set; }
}
