using Shared.Application.Events;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionResolver
{
    Task<Session> ResolveForBotAsync(BotIncomingMessage message, Guid botId, CancellationToken ct);

    Task CloseSessionAsync(Guid sessionId, CancellationToken ct);

    Task CloseSessionAndHierarchyAsync(Guid sessionId, CancellationToken ct);
}
