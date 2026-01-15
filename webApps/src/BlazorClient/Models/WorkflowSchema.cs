namespace BlazorClient.Models;

public record WorkflowSchema(
    List<NodeDefinition> Nodes,
    List<EdgeDefinition> Edges,
    List<LayoutDefinition> Layout);

public record NodeDefinition(Guid Id, string Type, string Label, object? Data);

public record EdgeDefinition(Guid From, Guid To, ConditionDefinition? Condition);

public record LayoutDefinition(Guid NodeId, double X, double Y);

public record ConditionDefinition(EqualsCondition? EqualsCondition);

public record EqualsCondition(string Left, string Right);