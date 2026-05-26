using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Entities;
using NotificationService.Enums;
using NotificationService.Interfaces;

namespace NotificationService.Repositories;

public class LiveChatSessionRepository(NotificationDbContext db) : ILiveChatSessionRepository
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

    public async Task<LiveChatSession?> TryTakeAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var rows = await db.LiveChatSessions
            .Where(s => s.Id == id && s.Status == LiveChatSessionStatus.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, LiveChatSessionStatus.InProgress)
                .SetProperty(x => x.OperatorId, operatorId)
                .SetProperty(x => x.TakenAt, DateTime.UtcNow)
                .SetProperty(x => x.ModifiedAt, DateTime.UtcNow),
                ct);

        return rows == 0 ? null : await db.LiveChatSessions.FindAsync([id], ct);
    }
}
