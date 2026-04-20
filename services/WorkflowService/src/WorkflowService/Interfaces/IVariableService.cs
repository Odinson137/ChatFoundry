using Shared.Domain.Enums;
using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IVariableService
{
    void PopulateFromEventParameters(Session session, IReadOnlyDictionary<MessageParameter, string> parameters);

    Task LoadClientVariablesAsync(Session session, CancellationToken ct);
    void SetVariable(Session session, string key, object? value);
    void SetAttribute(Session session, string attributeName, object? value);
    string? GetVariable(Session session, string key);
    Task SyncIfDirtyAsync(Session session, CancellationToken ct);
}
