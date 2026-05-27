using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowService.Interfaces;

namespace WorkflowService.Services.AiProviders;

public class OpenAiCompatibleProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _model;
    private readonly string _name;

    public string Name => _name;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_API_KEY" &&
        !string.IsNullOrWhiteSpace(_apiUrl);

    public OpenAiCompatibleProvider(
        IHttpClientFactory httpClientFactory,
        string httpClientName,
        string name,
        string apiKey,
        string apiUrl,
        string model)
    {
        _httpClientFactory = httpClientFactory;
        _httpClientName = httpClientName;
        _name = name;
        _apiKey = apiKey;
        _apiUrl = apiUrl;
        _model = model;
    }

    public async Task<AiCompletionResult> GetCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_httpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var requestBody = new ChatCompletionRequest(_model, messages.Select(m => new ChatMessage(m.Role, m.Content)), null);
        return await SendRequestAsync(client, requestBody, cancellationToken);
    }

    public async Task<AiCompletionResult> GetJsonCompletionAsync(
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_httpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var requestBody = new ChatCompletionRequest(
            _model,
            messages.Select(m => new ChatMessage(m.Role, m.Content)),
            new JsonResponseFormat("json_object"));

        return await SendRequestAsync(client, requestBody, cancellationToken);
    }

    private async Task<AiCompletionResult> SendRequestAsync(
        HttpClient client,
        ChatCompletionRequest requestBody,
        CancellationToken cancellationToken)
    {
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_apiUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);

        var responseContent = completionResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        var usage = completionResponse?.Usage;

        return new AiCompletionResult(responseContent, usage?.PromptTokens ?? 0, usage?.CompletionTokens ?? 0);
    }

    private record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IEnumerable<ChatMessage> Messages,
        [property: JsonPropertyName("response_format")] JsonResponseFormat? ResponseFormat);

    private record JsonResponseFormat([property: JsonPropertyName("type")] string Type);

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record UsageInfo(
        [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
        [property: JsonPropertyName("total_tokens")] int TotalTokens);

    private record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] List<Choice> Choices,
        [property: JsonPropertyName("usage")] UsageInfo? Usage);

    private record Choice([property: JsonPropertyName("message")] ChatMessage Message);
}
