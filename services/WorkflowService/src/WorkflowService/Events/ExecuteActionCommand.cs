namespace WorkflowService.Events;

public record ExecuteActionCommand(Guid ActionId, string ExternalUserId, string Channel);
