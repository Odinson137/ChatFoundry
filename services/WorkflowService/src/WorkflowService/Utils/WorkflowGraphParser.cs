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

    // ========= PARSE =========

    public WorkflowGraph Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var nodes = root.GetProperty("nodes")
            .EnumerateArray()
            .Select(ParseNode)
            .ToDictionary(n => n.Id);

        var edges = root.GetProperty("edges")
            .EnumerateArray()
            .Select(ParseEdge)
            .ToList();

        return new WorkflowGraph(nodes, edges);
    }

    private WorkflowNode ParseNode(JsonElement el)
    {
        var id = el.GetProperty("id").GetGuid();
        var type = Enum.Parse<WorkflowNodeType>(
            el.GetProperty("type").GetString()!,
            ignoreCase: true);

        return new WorkflowNode
        {
            Id = id,
            Type = type,
            Label = el.GetProperty("label").GetString()!,
            Data = el.TryGetProperty("data", out var data)
                ? ParseNodeData(type, data)
                : NodeData.Empty
        };
    }

    private WorkflowEdge ParseEdge(JsonElement el)
    {
        return new WorkflowEdge
        {
            From = el.GetProperty("from").GetGuid(),
            To = el.GetProperty("to").GetGuid(),
            Condition = el.TryGetProperty("condition", out var c)
                ? ParseCondition(c)
                : null
        };
    }
    
    private static NodeData ParseNodeData(
        WorkflowNodeType type,
        JsonElement data)
    {
        return type switch
        {
            WorkflowNodeType.Message =>
                data.Deserialize<MessageNodeData>(JsonOptions)!,

            WorkflowNodeType.Ask =>
                data.Deserialize<AskNodeData>(JsonOptions)!,

            WorkflowNodeType.SubWorkflow =>
                data.Deserialize<SubWorkflowNodeData>(JsonOptions)!,

            _ => NodeData.Empty
        };
    }

    private static WorkflowCondition ParseCondition(JsonElement el)
    {
        if (el.TryGetProperty("equals", out var eq))
        {
            return new EqualsCondition
            {
                Left = eq.GetProperty("left").GetString()!,
                Right = eq.GetProperty("right").GetString()!
            };
        }

        if (el.TryGetProperty("and", out var and))
        {
            return new AndCondition
            {
                Conditions = and.EnumerateArray()
                    .Select(ParseCondition)
                    .ToList()
            };
        }

        if (el.TryGetProperty("or", out var or))
        {
            return new OrCondition
            {
                Conditions = or.EnumerateArray()
                    .Select(ParseCondition)
                    .ToList()
            };
        }

        throw new InvalidOperationException(
            $"Unknown condition: {el}");
    }

    // ========= SERIALIZE =========

    public string Serialize(WorkflowGraph graph)
    {
        var model = new
        {
            nodes = graph.Nodes.Values.Select(SerializeNode),
            edges = graph.Edges.Select(SerializeEdge)
        };

        return JsonSerializer.Serialize(model, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static object SerializeNode(WorkflowNode node)
    {
        return new
        {
            id = node.Id,
            type = node.Type.ToString(),
            label = node.Label,
            data = node.Data is EmptyNodeData ? null : node.Data
        };
    }

    private static object SerializeEdge(WorkflowEdge edge)
    {
        return new
        {
            from = edge.From,
            to = edge.To,
            condition = edge.Condition != null
                ? SerializeCondition(edge.Condition)
                : null
        };
    }

    private static object SerializeCondition(WorkflowCondition condition)
    {
        return condition switch
        {
            EqualsCondition eq => new
            {
                equals = new
                {
                    left = eq.Left,
                    right = eq.Right
                }
            },

            AndCondition and => new
            {
                and = and.Conditions.Select(SerializeCondition)
            },

            OrCondition or => new
            {
                or = or.Conditions.Select(SerializeCondition)
            },

            _ => throw new InvalidOperationException(
                $"Unknown condition type {condition.GetType().Name}")
        };
    }
}
