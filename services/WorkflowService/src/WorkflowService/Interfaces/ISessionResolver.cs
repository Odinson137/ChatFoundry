using Shared.Application.Events;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface ISessionResolver
{
    /// <summary>
    /// Finds or creates an active session for the given channel, user and bot.
    /// </summary>
    Task<Session> ResolveForBotAsync(BotIncomingMessage message, Guid botId, CancellationToken ct);

    Task CloseSessionAsync(Guid sessionId, CancellationToken ct);

    /// <summary>
    /// Closes the given session and its entire hierarchy (all descendants and ancestors) due to an error.
    /// All affected sessions are marked as <see cref="SessionStatus.Failed"/> (not completed normally).
    /// </summary>
    Task CloseSessionAndHierarchyAsync(Guid sessionId, CancellationToken ct);
}