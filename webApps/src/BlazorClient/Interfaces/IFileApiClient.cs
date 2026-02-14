namespace BlazorClient.Interfaces;

/// <summary>
/// Результат загрузки файла в хранилище.
/// </summary>
public record FileUploadResult(string Key, string Url);

/// <summary>
/// Элемент списка файлов в хранилище (для выбора в блоке Медиа).
/// </summary>
public record FileInfoDto(string Key, string? Name, string? Url);

public interface IFileApiClient
{
    /// <summary>
    /// Загрузить файл в хранилище. Возвращает ключ и URL.
    /// </summary>
    Task<FileUploadResult?> UploadFileAsync(Stream content, string fileName, string? contentType, Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);

    /// <summary>
    /// Список файлов через GraphQL (по companyId и опционально workflowId).
    /// </summary>
    Task<List<FileInfoDto>> ListFilesAsync(Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);
}
