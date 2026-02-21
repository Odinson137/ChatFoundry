using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class WorkflowRepository(WorkflowDbContext db) : IWorkflowRepository
{
    public async Task<BotWorkflow?> GetActiveWorkflowAsync(Guid botId, CancellationToken ct)
    {
        return await db.Workflows
            .Where(x => x.BotId == botId)
            .Where(c => c.IsActiveBotWorkflow)
            .FirstOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<BotWorkflow?> GetByIdAsync(Guid id)
    {
        return await db.Workflows.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveAsync(BotWorkflow workflow)
    {
        if (db.Entry(workflow).State == EntityState.Detached)
            db.Workflows.Add(workflow);

        await db.SaveChangesAsync();
    }

    public async Task<BotWorkflow?> GetActionWorkflowAsync(Guid actionId)
    {
        var action = await db.Actions
            .Include(a => a.Session)
            .ThenInclude(s => s.Workflow)
            .ThenInclude(w => w.Bot)
            .FirstOrDefaultAsync(a => a.Id == actionId);
        return action?.Session?.Workflow;
    }
}