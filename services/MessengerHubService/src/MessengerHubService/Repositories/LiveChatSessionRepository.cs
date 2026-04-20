using Microsoft.EntityFrameworkCore;
using MessengerHubService.Data;
using MessengerHubService.Entities;
using MessengerHubService.Enums;
using MessengerHubService.Interfaces;

namespace MessengerHubService.Repositories;

public class LiveChatSessionRepository(MessengerHubDbContext db) : ILiveChatSessionRepository
{
    public async Task<LiveChatSession?> GetActiveByChannelAndClientAsync(
        Guid channelId, string clientId, CancellationToken ct)
    {
        return await db.LiveChatSessions
            .FirstOrDefaultAsync(s => s.ChannelId == channelId
                && s.ExternalUserId == clientId
                && s.Status == LiveChatSessionStatus.InProgress, ct);
    }

    public async Task<LiveChatSession?> GetWithIncludesAsync(Guid id, CancellationToken ct)
    {
        return await db.LiveChatSessions
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task AddAsync(LiveChatSession session, CancellationToken ct)
    {
        await db.LiveChatSessions.AddAsync(session, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(LiveChatSession session, CancellationToken ct)
    {
        db.LiveChatSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LiveChatSession>> GetActiveSessionsAsync(CancellationToken ct)
    {
        return await db.LiveChatSessions
            .Where(s => s.Status == LiveChatSessionStatus.Queued || s.Status == LiveChatSessionStatus.InProgress)
            .ToListAsync(ct);
    }
}
