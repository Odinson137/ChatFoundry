namespace WorkflowService.Interfaces;

public record AiCompletionResult(string Content, int PromptTokens, int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }

    Task<AiCompletionResult> GetCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken);

    Task<AiCompletionResult> GetJsonCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken);
}
