using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class LiveChatStateService
{
    public List<LiveChatSessionDto> QueuedChats { get; set; } = [];
    public List<LiveChatSessionDto> MyChats { get; set; } = [];
    public Guid? SelectedChatId { get; set; }

    public LiveChatSessionDto? GetSelectedChat()
    {
        if (SelectedChatId == null) return null;
        return QueuedChats.FirstOrDefault(c => c.Id == SelectedChatId)
               ?? MyChats.FirstOrDefault(c => c.Id == SelectedChatId);
    }

    public void AddOrUpdateFromSignalR(Guid liveChatSessionId, string externalUserId, string clientName, string channel, Guid channelId, string? preview = null)
    {
        var existing = QueuedChats.FirstOrDefault(c => c.Id == liveChatSessionId);
        if (existing == null)
        {
            QueuedChats.Insert(0, new LiveChatSessionDto
            {
                Id = liveChatSessionId,
                ExternalUserId = externalUserId,
                ClientFirstName = clientName,
                Channel = channel,
                ChannelId = channelId,
                LastMessagePreview = preview,
                Status = "Queued",
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public void MarkChatTaken(Guid liveChatSessionId)
    {
        var chat = QueuedChats.FirstOrDefault(c => c.Id == liveChatSessionId);
        if (chat != null)
        {
            QueuedChats.Remove(chat);
            MyChats.Insert(0, chat);
        }
    }

    public void UpdateLastMessagePreview(Guid liveChatSessionId, string preview)
    {
        var chat = MyChats.FirstOrDefault(c => c.Id == liveChatSessionId)
                   ?? QueuedChats.FirstOrDefault(c => c.Id == liveChatSessionId);
        if (chat != null)
        {
            chat.LastMessagePreview = preview;
        }
    }

    public void RemoveChat(Guid liveChatSessionId)
    {
        QueuedChats.RemoveAll(c => c.Id == liveChatSessionId);
        MyChats.RemoveAll(c => c.Id == liveChatSessionId);
        if (SelectedChatId == liveChatSessionId)
            SelectedChatId = null;
    }
}
