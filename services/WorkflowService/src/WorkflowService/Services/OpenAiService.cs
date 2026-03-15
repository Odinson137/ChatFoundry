using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using WorkflowService.Options;

namespace WorkflowService.Services;

public class OpenAiService : IOpenAiService
{
    private const int MaxContextChars = 12_000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;

    public OpenAiService(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> GetCompletionAsync(
        string prompt,
        IReadOnlyList<(string Role, string Content)>? chatHistory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.ApiKey == "YOUR_API_KEY")
        {
            // TODO залогать
            return string.Empty;
        }

        var apiMessages = BuildMessages(prompt, chatHistory);

        var client = _httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var requestBody = new ChatCompletionRequest(_options.Model, apiMessages);
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(_options.ApiUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);

            return completionResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        }
        catch (HttpRequestException e)
        {
            // TODO: Log the exception
            return $"Error: Could not get a response from OpenAI. {e.Message}";
        }
        catch (JsonException e)
        {
            // TODO: Log the exception
            return $"Error: Could not parse the response from OpenAI. {e.Message}";
        }
    }

    private static List<ChatMessage> BuildMessages(string prompt, IReadOnlyList<(string Role, string Content)>? chatHistory)
    {
        if (chatHistory is null || chatHistory.Count == 0)
            return [new ChatMessage("user", prompt)];

        (string Role, string Content)? systemMsg = null;
        IReadOnlyList<(string Role, string Content)> rest = chatHistory;
        if (chatHistory.Count > 0 && string.Equals(chatHistory[0].Role, "system", StringComparison.OrdinalIgnoreCase))
        {
            systemMsg = chatHistory[0];
            rest = chatHistory.Skip(1).ToList();
        }

        var truncated = TruncateHistory(rest, MaxContextChars);
        var list = truncated
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();
        if (systemMsg is { } sm)
            list.Insert(0, new ChatMessage(sm.Role, sm.Content));
        list.Add(new ChatMessage("user", prompt));
        return list;
    }

    /// <summary>
    /// Drops oldest messages from the start until total content length is at most maxChars.
    /// </summary>
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

    private record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IEnumerable<ChatMessage> Messages);
    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatCompletionResponse([property: JsonPropertyName("choices")] List<Choice> Choices);
    private record Choice([property: JsonPropertyName("message")] ChatMessage Message);
}
