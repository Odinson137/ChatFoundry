using System.Text.Json.Serialization;

namespace BlazorClient.Models;

public record WorkflowSchema(
    List<NodeDefinition> Nodes,
    List<EdgeDefinition> Edges,
    List<LayoutDefinition> Layout);

public record NodeDefinition(Guid Id, string Type, string Label, NodeData? Data);

public record EdgeDefinition(Guid From, Guid To, string? Label, ConditionDefinition? Condition);

public record LayoutDefinition(Guid NodeId, double X, double Y);

public class ConditionDefinition
{
#pragma warning disable CS0108, CS0114
    public EqualsCondition? Equals { get; set; }
#pragma warning restore CS0108, CS0114
    public ContainsCondition? Contains { get; set; }

    public ConditionDefinition() { }

    public ConditionDefinition(EqualsCondition? equals = null, ContainsCondition? contains = null)
    {
        Equals = equals;
        Contains = contains;
    }
}

public class EqualsCondition
{
    public string Left { get; set; } = string.Empty;
    public string Right { get; set; } = string.Empty;

    public EqualsCondition() { }
    public EqualsCondition(string left, string right) 
    { 
        Left = left; 
        Right = right; 
    }
}

public class ContainsCondition
{
    public string Left { get; set; } = string.Empty;
    public string Right { get; set; } = string.Empty;

    public ContainsCondition() { }
    public ContainsCondition(string left, string right) 
    { 
        Left = left; 
        Right = right; 
    }
}