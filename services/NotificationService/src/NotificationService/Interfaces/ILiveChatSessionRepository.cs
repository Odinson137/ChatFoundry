using NotificationService.Entities;
using NotificationService.Enums;

namespace NotificationService.Interfaces;

public interface ILiveChatSessionRepository
{
    Task<LiveChatSession?> GetActiveByChannelAndClientAsync(Guid channelId, string clientId, CancellationToken ct);
    Task<LiveChatSession?> GetWithIncludesAsync(Guid id, CancellationToken ct);
    Task AddAsync(LiveChatSession session, CancellationToken ct);
    Task SaveAsync(LiveChatSession session, CancellationToken ct);
    Task<IReadOnlyList<LiveChatSession>> GetActiveSessionsAsync(CancellationToken ct);
    Task<LiveChatSession?> TryTakeAsync(Guid id, Guid operatorId, CancellationToken ct);
}
