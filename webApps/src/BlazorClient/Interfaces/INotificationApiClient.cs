using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface INotificationApiClient
{
    Task<List<LiveChatSessionDto>> GetLiveChatSessionsAsync(string? status = null);
    Task<LiveChatSessionDto?> GetLiveChatSessionAsync(Guid id);
    Task TakeLiveChatAsync(Guid liveChatSessionId);
    Task SendLiveChatMessageAsync(Guid liveChatSessionId, string text);
    Task SendLiveChatMessageAsync(Guid liveChatSessionId, string text, string messageKind, string? caption = null);
    Task CloseLiveChatAsync(Guid liveChatSessionId);
    Task<LiveChatSessionDto> StartProactiveChatAsync(string externalUserId, Guid channelId, Guid? channelClientId, string channel);
}
