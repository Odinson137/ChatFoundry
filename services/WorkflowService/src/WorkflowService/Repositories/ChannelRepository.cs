using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class ChannelRepository(WorkflowDbContext db) : IChannelRepository
{
    public async Task<MessengerChannel?> GetByIdAsync(Guid channelId, CancellationToken ct = default)
    {
        return await db.MessengerChannels
            .FirstOrDefaultAsync(c => c.Id == channelId, ct);
    }

    public async Task<(string? Token, Guid? CompanyId)> GetTokenAndCompanyIdAsync(Guid channelId, CancellationToken ct = default)
    {
        var channel = await db.MessengerChannels
            .Where(c => c.Id == channelId)
            .Select(c => new { c.Token, c.CompanyId })
            .FirstOrDefaultAsync(ct);
        return channel == null ? (null, null) : (channel.Token, channel.CompanyId);
    }

    public async Task<bool> HasLinkedBotsAsync(Guid channelId, CancellationToken ct = default)
    {
        return await db.BotChannels
            .AnyAsync(bc => bc.ChannelId == channelId, ct);
    }
}
