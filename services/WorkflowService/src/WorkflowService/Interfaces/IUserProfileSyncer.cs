using WorkflowService.Entities;

namespace WorkflowService.Interfaces;

public interface IUserProfileSyncer
{
    Task SyncAsync(Session session, CancellationToken ct);
}
