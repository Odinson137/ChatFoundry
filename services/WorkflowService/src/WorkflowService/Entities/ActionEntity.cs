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

    public MessageKind MessageKind { get; set; } = MessageKind.Unknown;

    public WorkflowNodeType WorkflowNodeType { get; set; }

    public ActionStatus Status { get; set; } = ActionStatus.Pending;

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

