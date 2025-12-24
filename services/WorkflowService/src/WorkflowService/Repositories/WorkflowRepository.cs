using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class WorkflowRepository(WorkflowDbContext db) : IWorkflowRepository
{
    public async Task<Workflow?> GetActiveWorkflowAsync(Guid botId, CancellationToken ct)
    {
        return await db.Workflows
            .Where(x => x.BotId == botId)
            .Where(c => c.IsActiveBotWorkflow)
            .FirstOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<Workflow?> GetByIdAsync(Guid id)
    {
        return await db.Workflows.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveAsync(Workflow workflow)
    {
        if (db.Entry(workflow).State == EntityState.Detached)
            db.Workflows.Add(workflow);

        await db.SaveChangesAsync();
    }

    public async Task<Workflow?> GetActionWorkflowAsync(Guid actionId)
    {
        return await db.Actions.Where(c => c.Id == actionId).Select(x => x.Session.Workflow).FirstOrDefaultAsync();
    }
}