using Shared.Domain.Enums;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IVariableService
{
    /// <summary>
    /// Заполняет переменные сессии из параметров входящего сообщения (имя, username и т.д.).
    /// Вызывать перед LoadClientVariablesAsync, чтобы при race condition с ClientService были хотя бы эти данные.
    /// </summary>
    void PopulateFromEventParameters(Session session, IReadOnlyDictionary<MessageParameter, string> parameters);

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
