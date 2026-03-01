using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace WorkflowService.Entities;

public class Session : EntityBase
{
    public Guid WorkflowId { get; set; }
    public BotWorkflow Workflow { get; set; } = null!;

    public string ClientId { get; set; } = null!;
    public DefaultChannel Channel { get; set; }
    public Guid ChannelId { get; set; }
    public MessengerChannel MessengerChannel { get; set; } = null!;

    public Guid? CurrentNodeId { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public DateTime? CompletedAt { get; set; }

    public Guid? ParentSessionId { get; set; }
    public Session? ParentSession { get; set; }

    /// <summary>
    /// The SubWorkflow action in the parent session that spawned this child.
    /// Used to resume the correct node after child completes.
    /// </summary>
    public Guid? ParentActionId { get; set; }

    public int Depth { get; set; }

    [NotMapped]
    public bool ClientProfileDirty { get; set; }

    public List<ActionEntity> Actions { get; set; } = [];

    public Dictionary<string, string> Variables { get; set; } = new();

    public void MoveTo(Guid nextNodeId)
    {
        CurrentNodeId = nextNodeId;
    }
}
