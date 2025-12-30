using Shared.Domain.Entities;

namespace WorkflowService.Entities;

public class WorkflowVersion : EntityBase
{
    public Guid WorkflowId { get; set; }
    public BotWorkflow Workflow { get; set; } = null!;

    public string SchemaJson { get; set; } = "{}";

    public int Version { get; set; }
}