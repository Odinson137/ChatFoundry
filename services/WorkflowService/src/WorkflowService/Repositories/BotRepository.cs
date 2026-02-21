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

    public async Task<(string? Token, Guid? CompanyId)> GetBotTokenAndCompanyIdAsync(Guid botId, CancellationToken cancellationToken)
    {
        var bot = await db.Bots
            .Where(c => c.Id == botId)
            .Select(c => new { c.Token, c.CompanyId })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        return bot == null ? (null, null) : (bot.Token, bot.CompanyId);
    }
}