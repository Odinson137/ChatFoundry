using Shared.Domain.Entities;

namespace WorkflowService.Entities;

public class BotWorkflow : EntityBase
{
    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;

    public string NodesDefinition { get; set; } = "[]";
    public string EdgesDefinition { get; set; } = "[]";
    public string LayoutDefinition { get; set; } = "[]";

    public int Version { get; set; } = 1;
    
    public bool IsActiveBotWorkflow { get; set; } = false;

    public ICollection<Session> Sessions { get; set; } = [];
}