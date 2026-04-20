using Microsoft.Extensions.Caching.Distributed;
using NotificationService.Interfaces;

namespace NotificationService.Services;

public static class LiveChatRedisKeys
{
    public static string LiveChat(Guid channelId, string clientId) => $"livechat:{channelId}:{clientId}";
}

public class LiveChatService(
    ILiveChatSessionRepository repository,
    IDistributedCache distributedCache)
{
    public async Task SetRedisFlagAsync(Guid channelId, string clientId, Guid liveChatSessionId, CancellationToken ct)
    {
        var key = LiveChatRedisKeys.LiveChat(channelId, clientId);
        await distributedCache.SetStringAsync(key, liveChatSessionId.ToString(), ct);
    }

    public async Task RemoveRedisFlagAsync(Guid channelId, string clientId, CancellationToken ct)
    {
        var key = LiveChatRedisKeys.LiveChat(channelId, clientId);
        await distributedCache.RemoveAsync(key, ct);
    }

    public async Task RepopulateRedisFromDbAsync(CancellationToken ct)
    {
        var activeSessions = await repository.GetActiveSessionsAsync(ct);
        foreach (var session in activeSessions)
        {
            await SetRedisFlagAsync(session.ChannelId, session.ExternalUserId, session.Id, ct);
        }
    }
}
