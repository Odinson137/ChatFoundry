using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class BotRepository(WorkflowDbContext db) : IBotRepository
{
    public async Task<string?> GetBotTokenAsync(Guid botId, CancellationToken cancellationToken)
    {
        return await db.Bots.Where(c => c.Id == botId)
            .Select(c => c.Token)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}