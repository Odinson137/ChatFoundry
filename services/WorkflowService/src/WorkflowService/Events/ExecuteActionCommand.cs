namespace WorkflowService.Events;

public record ExecuteActionCommand(Guid ActionId, string ClientId, string Channel);
