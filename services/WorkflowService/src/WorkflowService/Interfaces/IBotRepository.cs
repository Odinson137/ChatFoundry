namespace WorkflowService.Interfaces;

public interface IBotRepository
{
    Task<string?> GetBotTokenAsync(Guid botId, CancellationToken cancellationToken);

    Task<(string? Token, Guid? CompanyId)> GetBotTokenAndCompanyIdAsync(Guid botId, CancellationToken cancellationToken);
}