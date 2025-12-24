using Newtonsoft.Json;
using WorkflowService.Utils;

namespace WorkflowService.Models.Workflow;

[JsonConverter(typeof(WorkflowConditionJsonConverter))]
public abstract class WorkflowCondition;

public sealed class EqualsCondition : WorkflowCondition
{
    public string Left { get; init; } = null!;
    public string Right { get; init; } = null!;
}

public sealed class AndCondition : WorkflowCondition
{
    public IReadOnlyList<WorkflowCondition> Conditions { get; init; } = [];
}

public sealed class OrCondition : WorkflowCondition
{
    public IReadOnlyList<WorkflowCondition> Conditions { get; init; } = [];
}