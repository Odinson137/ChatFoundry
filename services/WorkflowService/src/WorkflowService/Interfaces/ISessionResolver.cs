using Shared.Application.Events;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionResolver
{
    Task<Session> ResolveAsync(
        BotIncomingMessage message,
        CancellationToken ct);

    Task CloseSessionAsync(Guid sessionId, CancellationToken ct);
}