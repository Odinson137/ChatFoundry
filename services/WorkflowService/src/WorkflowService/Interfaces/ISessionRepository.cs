using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionRepository
{
    Task<Session?> FindActiveAsync(Guid channelId, string clientId, Guid botId, CancellationToken ct);

    Task<Session?> FindActiveChildAsync(Guid parentSessionId, CancellationToken ct);

    Task<Session?> GetAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Gets all sessions whose parent is the given session (any status).</summary>
    Task<IReadOnlyList<Session>> GetByParentSessionIdAsync(Guid parentSessionId, CancellationToken ct = default);

    Task AddAsync(Session session, CancellationToken ct = default);

    Task SaveAsync(Session session, CancellationToken ct = default);
}