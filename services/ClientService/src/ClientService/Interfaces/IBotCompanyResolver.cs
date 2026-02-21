namespace ClientService.Interfaces;

public interface IBotCompanyResolver
{
    Task<Guid?> GetCompanyIdByBotIdAsync(Guid botId, CancellationToken ct = default);
}
