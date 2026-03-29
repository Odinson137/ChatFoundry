namespace BlazorClient.Interfaces;

public record FileUploadResult(string Id);

public record FileInfoDto(
    string Id,
    string? Name,
    string? ContentType = null,
    long? Size = null,
    DateTime? CreatedAt = null,
    string? Key = null,
    Guid? UploadedByUserId = null,
    Guid? UploadedClientId = null)
{
    /// <summary>«Клиент» при загрузке от клиента; иначе «Администратор» (загрузка из панели / сервиса).</summary>
    public string UploadSourceDisplay =>
        UploadedClientId.HasValue ? "Клиент" : "Администратор";
}

public interface IFileApiClient
{
    Task<FileUploadResult?> UploadFileAsync(Stream content, string fileName, string? contentType, Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);

    Task<List<FileInfoDto>> ListFilesAsync(Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);

    Task<FileInfoDto?> GetFileAsync(Guid id, CancellationToken ct = default);

    Task DeleteFileAsync(Guid id, CancellationToken ct = default);

    Task<string?> GetDownloadUrlAsync(string key, bool forceDownload = false, CancellationToken ct = default);
}
