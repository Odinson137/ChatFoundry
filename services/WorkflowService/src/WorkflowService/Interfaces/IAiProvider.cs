namespace WorkflowService.Interfaces;

public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }

    Task<string> GetCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken);

    Task<string> GetJsonCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken);
}
