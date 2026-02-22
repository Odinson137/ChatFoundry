using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class BotRepository(WorkflowDbContext db) : IBotRepository
{
    public async Task<IReadOnlyList<Guid>> GetBotIdsByChannelIdAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        return await db.BotChannels
            .Where(bc => bc.ChannelId == channelId)
            .Select(bc => bc.BotId)
            .ToListAsync(cancellationToken);
    }
}