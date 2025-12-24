using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Repositories;

public class SessionRepository(WorkflowDbContext db) : ISessionRepository
{
    public Task<Session?> FindActiveAsync(string clientId, string channel, CancellationToken ct)
    {
        return db.Sessions.Include(c => c.Workflow)
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Channel == channel && c.Status == SessionStatus.Active, ct);
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