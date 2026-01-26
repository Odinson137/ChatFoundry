namespace WorkflowService.Services;

public interface IOpenAiService
{
    Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
