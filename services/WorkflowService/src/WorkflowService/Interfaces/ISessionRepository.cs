using Shared.Domain.Enums;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionRepository
{
    Task<Session?> FindActiveAsync(string clientId,
        DefaultChannel channel,
        CancellationToken ct);

    Task<Session?> GetAsync(Guid sessionId, CancellationToken ct = default);

    Task AddAsync(Session session, CancellationToken ct = default);

    Task SaveAsync(Session session, CancellationToken ct = default);
    Task<string?> GetBotTokenAsync(string clientId, CancellationToken cancellationToken);
}