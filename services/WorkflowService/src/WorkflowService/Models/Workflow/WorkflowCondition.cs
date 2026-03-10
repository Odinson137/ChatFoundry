using Newtonsoft.Json;
using WorkflowService.Utils;

namespace WorkflowService.Models.Workflow;

[JsonConverter(typeof(WorkflowConditionJsonConverter))]
public abstract class WorkflowCondition;

/// <summary>Базовый класс для условий с двумя операндами (Left, Right).</summary>
public abstract class BinaryCondition : WorkflowCondition
{
    public string Left { get; init; } = null!;
    public string Right { get; init; } = null!;
    /// <summary>Не учитывать регистр при сравнении строк. null = тип по умолчанию (для Contains/StartsWith/EndsWith/InList — true, для остальных — false).</summary>
    public bool? IgnoreCase { get; init; }
}

/// <summary>Условие с одним операндом (например, пусто/не пусто).</summary>
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
/// <summary>Left — проверяемое значение, Right — список через запятую (или один элемент).</summary>
public sealed class InListCondition : BinaryCondition;
public sealed class IsEmptyCondition : UnaryCondition;
public sealed class IsNotEmptyCondition : UnaryCondition;
/// <summary>Отрицание одного вложенного условия.</summary>
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