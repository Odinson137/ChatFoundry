namespace Shared.Domain.Enums;

public enum SessionStatus
{
    Active,
    WaitingForSubWorkflow,
    Completed,
    Failed,
    Cancelled,
    WaitingForWebhook,
}