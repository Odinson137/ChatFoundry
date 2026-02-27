using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlazorClient.Configuration;
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

        var response = await http.PostAsync($"{ApiEndpoints.Api}/file/files", form, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var id = json.TryGetProperty("id", out var idProp) ? idProp.GetGuid().ToString() : null;
        return id != null ? new FileUploadResult(id) : null;
    }

    public async Task<List<FileInfoDto>> ListFilesAsync(Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default)
    {
        try
        {
            var query = """
                query GetFiles {
                  files {
                    id
                    originalFileName
                    contentType
                    size
                    createdAt
                    key
                  }
                }
                """;
            var body = new { query };
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/file/graphql") { Content = content };
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
                list.Add(ParseFileEntity(f));
            }
            return list;
        }
        catch
        {
            return new List<FileInfoDto>();
        }
    }

    public async Task<FileInfoDto?> GetFileAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var query = """
                query GetFile($id: UUID!) {
                  file(id: $id) {
                    id
                    originalFileName
                    contentType
                    size
                    createdAt
                    key
                  }
                }
                """;
            var body = new { query, variables = new { id } };
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{ApiEndpoints.Api}/file/graphql", content, ct);
            response.EnsureSuccessStatusCode();
            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
            if (!doc.TryGetProperty("data", out var data) || !data.TryGetProperty("file", out var file) || file.ValueKind == JsonValueKind.Null)
                return null;
            return ParseFileEntity(file);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{ApiEndpoints.Api}/file/files/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetDownloadUrlAsync(string key, bool forceDownload = false, CancellationToken ct = default)
    {
        try
        {
            var downloadParam = forceDownload ? "&download=true" : "";
            var response = await http.GetFromJsonAsync<JsonElement>(
                $"{ApiEndpoints.Api}/file/files/url?key={Uri.EscapeDataString(key)}{downloadParam}", ct);
            return response.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static FileInfoDto ParseFileEntity(JsonElement f)
    {
        var id = f.TryGetProperty("id", out var idProp) ? idProp.GetGuid().ToString() : "";
        var name = f.TryGetProperty("originalFileName", out var n) ? n.GetString() : null;
        var contentType = f.TryGetProperty("contentType", out var ct) ? ct.GetString() : null;
        long? size = f.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt64() : null;
        DateTime? createdAt = f.TryGetProperty("createdAt", out var ca) && ca.ValueKind == JsonValueKind.String
            ? DateTime.TryParse(ca.GetString(), out var dt) ? dt : null
            : null;
        var key = f.TryGetProperty("key", out var k) ? k.GetString() : null;
        return new FileInfoDto(id, name, contentType, size, createdAt, key);
    }
}
