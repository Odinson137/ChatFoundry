using System.Text.Json;
using WorkflowService.Enums;
using WorkflowService.Models.Node;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public sealed class WorkflowGraphParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkflowGraph Parse(string? nodesJson, string? edgesJson)
    {
        var nodes = ParseNodes(nodesJson);
        var edges = ParseEdges(edgesJson);

        return new WorkflowGraph(nodes, edges);
    }

    private Dictionary<Guid, WorkflowNode> ParseNodes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "{}")
            return new Dictionary<Guid, WorkflowNode>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(ParseNode)
                .ToDictionary(n => n.Id);
        }
        catch
        {
            return new Dictionary<Guid, WorkflowNode>();
        }
    }

    private List<WorkflowEdge> ParseEdges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return new List<WorkflowEdge>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(ParseEdge)
                .ToList();
        }
        catch
        {
            return new List<WorkflowEdge>();
        }
    }

    private List<NodeLayout> ParseLayout(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return new List<NodeLayout>();

        try
        {
            return JsonSerializer.Deserialize<List<NodeLayout>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new List<NodeLayout>();
        }
    }

    // ========= Вспомогательные методы парсинга элементов =========

    private WorkflowNode ParseNode(JsonElement el)
    {
        var id = el.GetProperty("id").GetGuid();

        // Безопасный парсинг Enum
        var typeStr = el.TryGetProperty("type", out var t) ? t.GetString() : "Message";
        if (!Enum.TryParse<WorkflowNodeType>(typeStr, true, out var type))
            type = WorkflowNodeType.Message;

        return new WorkflowNode
        {
            Id = id,
            Type = type,
            Label = el.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
            Data = el.TryGetProperty("data", out var data)
                ? ParseNodeData(type, data)
                : NodeData.Empty
        };
    }

    private WorkflowEdge ParseEdge(JsonElement el)
    {
        return new WorkflowEdge
        {
            From = el.TryGetProperty("from", out var f) ? f.GetGuid() : Guid.Empty,
            To = el.TryGetProperty("to", out var t) ? t.GetGuid() : Guid.Empty,
            Condition = el.TryGetProperty("condition", out var c) && c.ValueKind != JsonValueKind.Null
                ? ParseCondition(c)
                : null
        };
    }

    private static NodeData ParseNodeData(WorkflowNodeType type, JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Null) return NodeData.Empty;

        return type switch
        {
            WorkflowNodeType.Message => data.Deserialize<MessageNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.Ask => data.Deserialize<AskNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.SubWorkflow => data.Deserialize<SubWorkflowNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.SetVariable => data.Deserialize<SetVariableNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.HttpRequest => data.Deserialize<HttpRequestNodeData>(JsonOptions) ?? NodeData.Empty,
            _ => NodeData.Empty
        };
    }

    private static WorkflowCondition ParseCondition(JsonElement el)
    {
        if (el.TryGetProperty("equals", out var eq))
        {
            return new EqualsCondition
            {
                Left = eq.TryGetProperty("left", out var l) ? l.GetString() ?? "" : "",
                Right = eq.TryGetProperty("right", out var r) ? r.GetString() ?? "" : ""
            };
        }

        if (el.TryGetProperty("and", out var and))
            return new AndCondition { Conditions = and.EnumerateArray().Select(ParseCondition).ToList() };

        if (el.TryGetProperty("or", out var or))
            return new OrCondition { Conditions = or.EnumerateArray().Select(ParseCondition).ToList() };

        throw new InvalidOperationException(
            $"Unknown condition: {el}");
    }

    public (string Nodes, string Edges) Serialize(WorkflowGraph graph)
    {
        var nodes = graph.Nodes.Values.Select(n => new
        {
            id = n.Id,
            type = n.Type.ToString(),
            label = n.Label,
            data = n.Data is EmptyNodeData ? null : n.Data
        });

        var edges = graph.Edges.Select(e => new
        {
            from = e.From,
            to = e.To,
            condition = e.Condition != null ? SerializeCondition(e.Condition) : null
        });

        return (
            JsonSerializer.Serialize(nodes, JsonOptions),
            JsonSerializer.Serialize(edges, JsonOptions)
        );
    }

    private static object SerializeCondition(WorkflowCondition condition)
    {
        return condition switch
        {
            EqualsCondition eq => new { equals = new { left = eq.Left, right = eq.Right } },
            AndCondition and => new { and = and.Conditions.Select(SerializeCondition) },
            OrCondition or => new { or = or.Conditions.Select(SerializeCondition) },
            _ => new { }
        };
    }
}

public record NodeLayout(Guid NodeId, double X, double Y);