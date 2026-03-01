using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class SessionRepository(WorkflowDbContext db) : ISessionRepository
{
    public Task<Session?> FindActiveAsync(Guid channelId, string clientId, Guid botId, CancellationToken ct)
    {
        return db.Sessions
            .Include(c => c.Workflow)
            .FirstOrDefaultAsync(
                c => c.ChannelId == channelId
                     && c.ClientId == clientId
                     && c.Workflow.BotId == botId
                     && (c.Status == SessionStatus.Active || c.Status == SessionStatus.WaitingForSubWorkflow),
                ct);
    }

    public Task<Session?> FindActiveChildAsync(Guid parentSessionId, CancellationToken ct)
    {
        return db.Sessions
            .Include(c => c.Workflow)
            .FirstOrDefaultAsync(
                c => c.ParentSessionId == parentSessionId
                     && (c.Status == SessionStatus.Active || c.Status == SessionStatus.WaitingForSubWorkflow),
                ct);
    }

    public async Task<Session?> GetAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await db.Sessions.Include(c => c.Workflow).FirstOrDefaultAsync(c => c.Id == sessionId, ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        await db.Sessions.AddAsync(session, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Session session, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }
}