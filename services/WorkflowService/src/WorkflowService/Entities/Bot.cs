using Shared.Domain.Entities;

namespace WorkflowService.Entities;

public class Bot : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    public ICollection<BotWorkflow> Workflows { get; set; } = [];
}