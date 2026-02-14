namespace BlazorClient.Interfaces;

/// <summary>
/// Результат загрузки файла в хранилище (ID файла в файловом сервисе).
/// </summary>
public record FileUploadResult(string Id);

/// <summary>
/// Элемент списка файлов в хранилище (для выбора в блоке Медиа).
/// </summary>
public record FileInfoDto(string Id, string? Name);

public interface IFileApiClient
{
    /// <summary>
    /// Загрузить файл в хранилище. Возвращает ID файла.
    /// </summary>
    Task<FileUploadResult?> UploadFileAsync(Stream content, string fileName, string? contentType, Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);

    /// <summary>
    /// Список файлов через GraphQL (по companyId и опционально workflowId).
    /// </summary>
    Task<List<FileInfoDto>> ListFilesAsync(Guid? companyId = null, Guid? workflowId = null, CancellationToken ct = default);
}
