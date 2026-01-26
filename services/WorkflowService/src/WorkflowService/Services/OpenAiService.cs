using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using WorkflowService.Options;

namespace WorkflowService.Services;

public class OpenAiService : IOpenAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;

    public OpenAiService(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.ApiKey == "YOUR_API_KEY")
        {
            // TODO залогать
            return string.Empty;
        }

        var client = _httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var requestBody = new ChatCompletionRequest(_options.Model, [new ChatMessage("user", prompt)]);
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

    private record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IEnumerable<ChatMessage> Messages);
    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatCompletionResponse([property: JsonPropertyName("choices")] List<Choice> Choices);
    private record Choice([property: JsonPropertyName("message")] ChatMessage Message);
}
