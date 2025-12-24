using Shared.Domain.Entities;

namespace WorkflowService.Entities;

public class Bot : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Workflow> Workflows { get; set; } = [];
}