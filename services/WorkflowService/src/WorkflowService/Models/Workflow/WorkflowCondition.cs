using Newtonsoft.Json;
using WorkflowService.Utils;

namespace WorkflowService.Models.Workflow;

[JsonConverter(typeof(WorkflowConditionJsonConverter))]
public abstract class WorkflowCondition;

public abstract class BinaryCondition : WorkflowCondition
{
    public string Left { get; init; } = null!;
    public string Right { get; init; } = null!;
    public bool? IgnoreCase { get; init; }
}

public abstract class UnaryCondition : WorkflowCondition
{
    public string Left { get; init; } = null!;
}

public sealed class EqualsCondition : BinaryCondition;
public sealed class NotEqualsCondition : BinaryCondition;
public class ContainsCondition : BinaryCondition;
public sealed class GreaterThanCondition : BinaryCondition;
public sealed class LessThanCondition : BinaryCondition;
public sealed class GreaterOrEqualCondition : BinaryCondition;
public sealed class LessOrEqualCondition : BinaryCondition;
public sealed class StartsWithCondition : BinaryCondition;
public sealed class EndsWithCondition : BinaryCondition;
public sealed class RegexMatchCondition : BinaryCondition;
public sealed class InListCondition : BinaryCondition;
public sealed class IsEmptyCondition : UnaryCondition;
public sealed class IsNotEmptyCondition : UnaryCondition;
public sealed class NotCondition : WorkflowCondition
{
    public WorkflowCondition Condition { get; init; } = null!;
}

public sealed class AndCondition : WorkflowCondition
{
    public IReadOnlyList<WorkflowCondition> Conditions { get; init; } = [];
}

public sealed class OrCondition : WorkflowCondition
{
    public IReadOnlyList<WorkflowCondition> Conditions { get; init; } = [];
}