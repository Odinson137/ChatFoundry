using System.Text.Json.Serialization;
using BlazorClient.Interfaces;

namespace BlazorClient.Models;

public record WorkflowSchema(
    List<NodeDefinition> Nodes,
    List<EdgeDefinition> Edges,
    List<LayoutDefinition> Layout,
    List<WorkflowParameterDto>? InputParameters = null,
    List<WorkflowParameterDto>? OutputParameters = null);

[JsonConverter(typeof(NodeDefinitionJsonConverter))]
public record NodeDefinition(Guid Id, string Type, string Label, NodeData? Data);

public record EdgeDefinition(Guid From, Guid To, string? Label, ConditionDefinition? Condition);

public record LayoutDefinition(Guid NodeId, double X, double Y);

public class ConditionDefinition
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable CS0108, CS0114
    public EqualsCondition? Equals { get; set; }
#pragma warning restore CS0108, CS0114
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContainsCondition? Contains { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotEqualsCondition? NotEquals { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GreaterThanCondition? GreaterThan { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LessThanCondition? LessThan { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GreaterOrEqualCondition? GreaterOrEqual { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LessOrEqualCondition? LessOrEqual { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StartsWithCondition? StartsWith { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EndsWithCondition? EndsWith { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RegexMatchCondition? Regex { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InListCondition? InList { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IsEmptyCondition? IsEmpty { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IsNotEmptyCondition? IsNotEmpty { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("and")]
    public List<ConditionDefinition>? And { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("or")]
    public List<ConditionDefinition>? Or { get; set; }

    public ConditionDefinition() { }

    /// <summary>Возвращает true, если это составное условие (И или ИЛИ).</summary>
    public bool IsComposite => (And != null && And.Count > 0) || (Or != null && Or.Count > 0);
    /// <summary>Список подусловий для редактирования (либо And, либо Or, либо null).</summary>
    public List<ConditionDefinition>? SubConditions => And ?? Or;
}

/// <summary>Базовый класс для условий с двумя операндами (Left, Right).</summary>
public class BinaryConditionBase
{
    public string Left { get; set; } = string.Empty;
    public string Right { get; set; } = string.Empty;
    /// <summary>Не учитывать регистр при сравнении.</summary>
    public bool? IgnoreCase { get; set; }
}

public class EqualsCondition : BinaryConditionBase
{
    public EqualsCondition() { }
    public EqualsCondition(string left, string right) { Left = left; Right = right; }
}

public class ContainsCondition : BinaryConditionBase
{
    public ContainsCondition() { }
    public ContainsCondition(string left, string right) { Left = left; Right = right; }
}

public class NotEqualsCondition : BinaryConditionBase
{
    public NotEqualsCondition() { }
    public NotEqualsCondition(string left, string right) { Left = left; Right = right; }
}

public class GreaterThanCondition : BinaryConditionBase
{
    public GreaterThanCondition() { }
    public GreaterThanCondition(string left, string right) { Left = left; Right = right; }
}

public class LessThanCondition : BinaryConditionBase
{
    public LessThanCondition() { }
    public LessThanCondition(string left, string right) { Left = left; Right = right; }
}

public class GreaterOrEqualCondition : BinaryConditionBase
{
    public GreaterOrEqualCondition() { }
    public GreaterOrEqualCondition(string left, string right) { Left = left; Right = right; }
}

public class LessOrEqualCondition : BinaryConditionBase
{
    public LessOrEqualCondition() { }
    public LessOrEqualCondition(string left, string right) { Left = left; Right = right; }
}

public class StartsWithCondition : BinaryConditionBase
{
    public StartsWithCondition() { }
    public StartsWithCondition(string left, string right) { Left = left; Right = right; }
}

public class EndsWithCondition : BinaryConditionBase
{
    public EndsWithCondition() { }
    public EndsWithCondition(string left, string right) { Left = left; Right = right; }
}

public class RegexMatchCondition : BinaryConditionBase
{
    public RegexMatchCondition() { }
    public RegexMatchCondition(string left, string right) { Left = left; Right = right; }
}

public class InListCondition : BinaryConditionBase
{
    public InListCondition() { }
    public InListCondition(string left, string right) { Left = left; Right = right; }
}

/// <summary>Условие с одним операндом (Left).</summary>
public class UnaryConditionBase
{
    public string Left { get; set; } = string.Empty;
}

public class IsEmptyCondition : UnaryConditionBase
{
    public IsEmptyCondition() { }
    public IsEmptyCondition(string left) { Left = left; }
}

public class IsNotEmptyCondition : UnaryConditionBase
{
    public IsNotEmptyCondition() { }
    public IsNotEmptyCondition(string left) { Left = left; }
}