using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IVariableService
{
    Task LoadClientVariablesAsync(Session session, CancellationToken ct);
    void SetVariable(Session session, string key, object? value);
    string? GetVariable(Session session, string key);
    Task SyncIfDirtyAsync(Session session, CancellationToken ct);
}
