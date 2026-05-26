using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class FileUrlResolver(HttpClient httpClient) : IFileUrlResolver
{
    public async Task<string?> GetUrlAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        try
        {
            var encodedKey = Uri.EscapeDataString(key);
            var response = await httpClient.GetAsync($"files/url?key={encodedKey}", ct);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<FileUrlResponse>(ct);
            return dto?.Url;
        }
        catch
        {
            return null;
        }
    }

    private record FileUrlResponse(string Url);
}
