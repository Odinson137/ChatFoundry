using Shared.Domain.Entities;

namespace WorkflowService.Entities;

public class Workflow : EntityBase
{
    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;

    public string SchemaJson { get; set; } = "{}";

    public int Version { get; set; } = 1;
    
    public bool IsActiveBotWorkflow { get; set; } = false;
}