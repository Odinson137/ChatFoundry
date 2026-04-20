namespace WorkflowService.Interfaces;

public interface IFileUrlResolver
{
    Task<string?> GetUrlAsync(string key, CancellationToken ct = default);
}
