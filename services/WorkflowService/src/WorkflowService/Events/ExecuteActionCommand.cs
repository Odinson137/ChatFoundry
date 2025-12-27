using Shared.Domain.Enums;

namespace WorkflowService.Events;

public record ExecuteActionCommand(Guid ActionId, string ExternalUserId, DefaultChannels Channel);
