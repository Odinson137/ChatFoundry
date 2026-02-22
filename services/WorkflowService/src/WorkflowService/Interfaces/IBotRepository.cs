namespace WorkflowService.Interfaces;

public interface IBotRepository
{
    Task<IReadOnlyList<Guid>> GetBotIdsByChannelIdAsync(Guid channelId, CancellationToken cancellationToken = default);
}