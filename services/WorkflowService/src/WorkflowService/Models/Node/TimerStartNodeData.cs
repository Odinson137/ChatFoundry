namespace WorkflowService.Models.Node;

public sealed class TimerStartNodeData : NodeData
{
    public string ScheduleType { get; init; } = "OneTime";
    public string? FireTimeUtc { get; init; }
    public string? CronExpression { get; init; }
    public string Timezone { get; init; } = "UTC";
    public ClientFilterCriteria? ClientFilter { get; init; }
}

public class ClientFilterCriteria
{
    public List<string> ClientIds { get; set; } = [];
    public List<ClientAttributeFilterCondition> AttributeConditions { get; set; } = [];
    public string Logic { get; set; } = "and";
    public List<Guid> Channels { get; set; } = [];
}

public class ClientAttributeFilterCondition
{
    public string AttributeKey { get; set; } = "";
    public string Operator { get; set; } = "equals";
    public string Value { get; set; } = "";
    public bool? IgnoreCase { get; set; }
}
