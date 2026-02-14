using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlazorClient.Interfaces;

namespace BlazorClient.Services;

public class FileApiClient(HttpClient http) : IFileApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<FileUploadResult?> UploadFileAsync(Stream content, string fileName, string? contentType, Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        form.Add(streamContent, "file", fileName);
        if (companyId.HasValue)
            form.Add(new StringContent(companyId.Value.ToString()), "companyId");
        if (workflowId.HasValue)
            form.Add(new StringContent(workflowId.Value.ToString()), "workflowId");

        var response = await http.PostAsync("file/files", form, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var key = json.TryGetProperty("key", out var k) ? k.GetString() : null;
        var url = json.TryGetProperty("url", out var u) ? u.GetString() : null;
        return key != null ? new FileUploadResult(key, url ?? key) : null;
    }

    public async Task<List<FileInfoDto>> ListFilesAsync(Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default)
    {
        try
        {
            var query = """
                query GetFiles {
                  files {
                    id
                    key
                    url
                    originalFileName
                  }
                }
                """;
            var body = new { query };
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "file/graphql") { Content = content };
            if (workflowId.HasValue)
                request.Headers.TryAddWithoutValidation("X-Workflow-Id", workflowId.Value.ToString());
            var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
            if (!doc.TryGetProperty("data", out var data) || !data.TryGetProperty("files", out var files))
                return new List<FileInfoDto>();
            var list = new List<FileInfoDto>();
            foreach (var f in files.EnumerateArray())
            {
                var key = f.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                var name = f.TryGetProperty("originalFileName", out var n) ? n.GetString() : null;
                var url = f.TryGetProperty("url", out var u) ? u.GetString() : null;
                list.Add(new FileInfoDto(key, name, url));
            }
            return list;
        }
        catch
        {
            return new List<FileInfoDto>();
        }
    }
}
