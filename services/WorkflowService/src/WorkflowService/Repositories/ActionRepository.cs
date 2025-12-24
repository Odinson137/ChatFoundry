using Microsoft.EntityFrameworkCore;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class ActionRepository(WorkflowDbContext db) : IActionRepository
{
    public async Task<ActionEntity?> GetAsync(string channel, string clientId, CancellationToken ct)
    {
        return await db.Actions
            .Include(c => c.Session)
            .ThenInclude(c => c.Workflow)
            .Where(x => x.Session.Channel == channel && x.Session.ClientId == clientId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ActionEntity?> GetAsync(
        Guid actionId,
        CancellationToken ct)
    {
        return await db.Actions
            .FirstOrDefaultAsync(x => x.Id == actionId, ct);
    }

    public async Task AddAsync(
        ActionEntity action,
        CancellationToken ct = default)
    {
        await db.Actions.AddAsync(action, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(
        ActionEntity action,
        CancellationToken ct = default)
    {
        db.Actions.Update(action);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(
        Guid sessionId,
        Guid nodeId,
        CancellationToken ct)
    {
        return await db.Actions.AnyAsync(
            x => x.SessionId == sessionId &&
                 x.NodeId == nodeId,
            ct);
    }
}