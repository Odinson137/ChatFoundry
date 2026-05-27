namespace WorkflowService.Interfaces;

public interface IOpenAiService
{
    Task<AiCompletionResult> GetCompletionAsync(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory = null,
        CancellationToken cancellationToken = default);

    Task<AiCompletionResult> GetJsonObjectCompletionAsync(
        string systemInstruction,
        string userContent,
        CancellationToken cancellationToken = default);
}
