namespace WorkflowService.Services;

public interface IOpenAiService
{
    /// <summary>
    /// Запрос к OpenAI. Если chatHistory не null — эти сообщения идут перед prompt (последний user message).
    /// История обрезается по лимиту символов, если превышает.
    /// </summary>
    Task<string> GetCompletionAsync(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory = null,
        CancellationToken cancellationToken = default);
}
