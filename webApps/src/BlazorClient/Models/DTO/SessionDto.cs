namespace BlazorClient.Models.DTO;

public class SessionDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public Guid ChannelId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid? CurrentNodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public SessionWorkflowDto? Workflow { get; set; }
    public List<SessionActionDto> Actions { get; set; } = [];
    public List<KeyValueEntryDto>? Variables { get; set; }

    public Dictionary<string, string> GetVariablesDict()
        => Variables?.ToDictionary(v => v.Key, v => v.Value) ?? new();
}

public class KeyValueEntryDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SessionWorkflowDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string NodesDefinition { get; set; } = "[]";
    public string EdgesDefinition { get; set; } = "[]";
    public string LayoutDefinition { get; set; } = "[]";
    public SessionBotDto? Bot { get; set; }
}

public class SessionBotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SessionActionDto
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string WorkflowNodeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
