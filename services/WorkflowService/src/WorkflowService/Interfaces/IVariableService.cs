using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IVariableService
{
    Task LoadClientVariablesAsync(Session session, CancellationToken ct);
    void SetVariable(Session session, string key, object? value);
    /// <summary>
    /// Записать значение в атрибут клиента (глобальный, между сессиями).
    /// Разрешено вызывать только из блока «Атрибут». Ключ без префикса $client. (например name, email).
    /// </summary>
    void SetAttribute(Session session, string attributeName, object? value);
    string? GetVariable(Session session, string key);
    Task SyncIfDirtyAsync(Session session, CancellationToken ct);
}
