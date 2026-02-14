namespace WorkflowService.Interfaces;

/// <summary>
/// Преобразует ключ файла в хранилище в URL для отдачи клиенту (Telegram и т.д.).
/// </summary>
public interface IFileUrlResolver
{
    Task<string?> GetUrlAsync(string key, CancellationToken ct = default);
}
