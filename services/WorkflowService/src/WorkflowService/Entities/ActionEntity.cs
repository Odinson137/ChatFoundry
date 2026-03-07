using Shared.Domain.Entities;
using Shared.Domain.Enums;
using WorkflowService.Enums;

namespace WorkflowService.Entities;

public class ActionEntity : EntityBase
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid NodeId { get; set; }
    public string? Payload { get; set; }

    public WorkflowNodeType WorkflowNodeType { get; set; }

    public ActionStatus Status { get; set; } = ActionStatus.Pending;

    /// <summary>
    /// Error message when Status is Failed (e.g. exception message from executor).
    /// </summary>
    public string? ErrorMessage { get; set; }

    public void MarkInProgress()
    {
        Status = ActionStatus.Processing;
    }

    public void MarkCompleted()
    {
        Status = ActionStatus.Completed;
    }
    
    public void MarkFailed()
    {
        Status = ActionStatus.Failed;
    }
}

