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
}