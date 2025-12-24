using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionRepository
{
    Task<Session?> FindActiveAsync(string clientId,
        string channel,
        CancellationToken ct);

    Task<Session?> GetAsync(Guid sessionId, CancellationToken ct = default);

    Task AddAsync(Session session, CancellationToken ct = default);

    Task SaveAsync(Session session, CancellationToken ct = default);
}