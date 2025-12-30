namespace WorkflowService.Interfaces;

public interface IBotRepository
{
    Task<string?> GetBotTokenAsync(Guid botId, CancellationToken cancellationToken);
}