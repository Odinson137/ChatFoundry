using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IChannelRepository
{
    Task<MessengerChannel?> GetByIdAsync(Guid channelId, CancellationToken ct = default);
    Task<(string? Token, Guid? CompanyId)> GetTokenAndCompanyIdAsync(Guid channelId, CancellationToken ct = default);
    Task<bool> HasLinkedBotsAsync(Guid channelId, CancellationToken ct = default);
}
