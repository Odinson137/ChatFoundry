using Shared.Domain.Enums;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IActionRepository
{
    Task<ActionEntity?> GetAsync(DefaultChannels channelId, string clientId, CancellationToken ct);
    Task<ActionEntity?> GetAsync(Guid actionId, CancellationToken ct);

    Task AddAsync(ActionEntity action, CancellationToken ct = default);

    Task SaveAsync(ActionEntity action, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid sessionId,
        Guid nodeId,
        CancellationToken ct);
}
