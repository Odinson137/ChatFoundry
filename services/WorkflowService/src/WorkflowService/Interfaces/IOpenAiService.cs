namespace WorkflowService.Services;

public interface IOpenAiService
{
    Task<string> GetCompletionAsync(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory = null,
        CancellationToken cancellationToken = default);

    Task<string> GetJsonObjectCompletionAsync(
        string systemInstruction,
        string userContent,
        CancellationToken cancellationToken = default);
}
