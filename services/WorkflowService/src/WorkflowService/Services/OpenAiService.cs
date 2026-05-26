using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class OpenAiService : IOpenAiService
{
    private const int MaxContextChars = 12_000;

    private readonly IEnumerable<IAiProvider> _providers;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(IEnumerable<IAiProvider> providers, ILogger<OpenAiService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<string> GetCompletionAsync(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(prompt, chatHistory);
        return await ExecuteWithFailoverAsync(
            p => p.GetCompletionAsync(messages, cancellationToken),
            cancellationToken);
    }

    public async Task<string> GetJsonObjectCompletionAsync(
        string systemInstruction,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<(string Role, string Content)>
        {
            ("system", systemInstruction),
            ("user", userContent)
        };

        return await ExecuteWithFailoverAsync(
            p => p.GetJsonCompletionAsync(messages, cancellationToken),
            cancellationToken);
    }

    private async Task<string> ExecuteWithFailoverAsync(
        Func<IAiProvider, Task<string>> execute,
        CancellationToken cancellationToken)
    {
        var configuredProviders = _providers.Where(p => p.IsConfigured).ToList();

        if (configuredProviders.Count == 0)
        {
            _logger.LogWarning("No AI providers are configured. Request will be skipped.");
            return string.Empty;
        }

        Exception? lastException = null;

        foreach (var provider in configuredProviders)
        {
            try
            {
                _logger.LogDebug("Trying AI provider: {ProviderName}", provider.Name);
                var result = await execute(provider);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "AI provider {ProviderName} failed: {Message}", provider.Name, ex.Message);

                if (cancellationToken.IsCancellationRequested)
                    throw;
            }
        }

        _logger.LogError(lastException, "All AI providers failed");
        throw lastException!;
    }

    private static List<(string Role, string Content)> BuildMessages(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory)
    {
        if (chatHistory is null || chatHistory.Count == 0)
            return [("user", prompt)];

        (string Role, string Content)? systemMsg = null;
        var rest = chatHistory;
        if (chatHistory.Count > 0 && string.Equals(chatHistory[0].Role, "system", StringComparison.OrdinalIgnoreCase))
        {
            systemMsg = chatHistory[0];
            rest = chatHistory.Skip(1).ToList();
        }

        var truncated = TruncateHistory(rest, MaxContextChars);
        var list = truncated.ToList();
        if (systemMsg is { } sm)
            list.Insert(0, sm);
        list.Add(("user", prompt));
        return list;
    }

    private static IReadOnlyList<(string Role, string Content)> TruncateHistory(
        IReadOnlyList<(string Role, string Content)> history,
        int maxChars)
    {
        var total = 0;
        for (var i = history.Count - 1; i >= 0; i--)
        {
            total += history[i].Content?.Length ?? 0;
            if (total > maxChars)
            {
                return history.Skip(i + 1).ToList();
            }
        }

        return history;
    }
}
