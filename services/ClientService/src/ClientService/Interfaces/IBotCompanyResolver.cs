namespace ClientService.Interfaces;

public interface IBotCompanyResolver
{
    Task<Guid?> GetCompanyIdByChannelIdAsync(Guid channelId, CancellationToken ct = default);
}
