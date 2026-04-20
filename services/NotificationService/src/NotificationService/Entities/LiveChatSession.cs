using Shared.Domain.Entities;
using Shared.Domain.Enums;
using NotificationService.Enums;

namespace NotificationService.Entities;

public class LiveChatSession : EntityBase
{
    public Guid? WorkflowSessionId { get; set; }
    public string ExternalUserId { get; set; } = null!;
    public DefaultChannel Channel { get; set; }
    public Guid ChannelId { get; set; }
    public Guid? BotId { get; set; }
    public string? BotName { get; set; }
    public Guid? CompanyId { get; set; }
    public string? ClientFirstName { get; set; }
    public string? ClientUserName { get; set; }
    public LiveChatSessionStatus Status { get; set; } = LiveChatSessionStatus.Queued;
    public Guid? OperatorId { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LastMessagePreview { get; set; }
}
