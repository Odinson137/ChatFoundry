namespace BlazorClient.Models.DTO;

public class LiveChatSessionDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowSessionId { get; set; }
    public string ExternalUserId { get; set; } = "";
    public string Channel { get; set; } = "";
    public Guid ChannelId { get; set; }
    public Guid? ClientChannelId { get; set; }
    public Guid? BotId { get; set; }
    public string? BotName { get; set; }
    public Guid? CompanyId { get; set; }
    public string? ClientFirstName { get; set; }
    public string? ClientUserName { get; set; }
    public string Status { get; set; } = "";
    public string? OperatorId { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTime CreatedAt { get; set; }
}
