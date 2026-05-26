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



    private WorkflowNode ParseNode(JsonElement el)
    {
        var id = el.GetProperty("id").GetGuid();


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
            WorkflowNodeType.SetAttribute => data.Deserialize<SetAttributeNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.HttpRequest => data.Deserialize<HttpRequestNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.AIGenerate => data.Deserialize<AIGenerateNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.Media => data.Deserialize<MediaNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.Wait => data.Deserialize<WaitNodeData>(JsonOptions) ?? NodeData.Empty,
            WorkflowNodeType.TimerStart => data.Deserialize<TimerStartNodeData>(JsonOptions) ?? NodeData.Empty,
            _ => NodeData.Empty
        };
    }

    private static WorkflowCondition ParseCondition(JsonElement el)
    {
        static void readBinary(JsonElement bin, out string left, out string right, out bool? ignoreCase)
        {
            left = bin.TryGetProperty("left", out var l) ? l.GetString() ?? "" : "";
            right = bin.TryGetProperty("right", out var r) ? r.GetString() ?? "" : "";
            ignoreCase = bin.TryGetProperty("ignoreCase", out var ic) ? (ic.ValueKind == JsonValueKind.True) : null;
        }

        if (el.TryGetProperty("equals", out var eq))
        {
            readBinary(eq, out var l, out var r, out var ic);
            return new EqualsCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("notEquals", out var ne))
        {
            readBinary(ne, out var l, out var r, out var ic);
            return new NotEqualsCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("contains", out var c))
        {
            readBinary(c, out var l, out var r, out var ic);
            return new ContainsCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("greaterThan", out var gt))
        {
            readBinary(gt, out var l, out var r, out var ic);
            return new GreaterThanCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("lessThan", out var lt))
        {
            readBinary(lt, out var l, out var r, out var ic);
            return new LessThanCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("greaterOrEqual", out var ge))
        {
            readBinary(ge, out var l, out var r, out var ic);
            return new GreaterOrEqualCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("lessOrEqual", out var le))
        {
            readBinary(le, out var l, out var r, out var ic);
            return new LessOrEqualCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("startsWith", out var sw))
        {
            readBinary(sw, out var l, out var r, out var ic);
            return new StartsWithCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("endsWith", out var ew))
        {
            readBinary(ew, out var l, out var r, out var ic);
            return new EndsWithCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("regex", out var rx))
        {
            readBinary(rx, out var l, out var r, out var ic);
            return new RegexMatchCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("inList", out var il))
        {
            readBinary(il, out var l, out var r, out var ic);
            return new InListCondition { Left = l, Right = r, IgnoreCase = ic };
        }
        if (el.TryGetProperty("isEmpty", out var ie))
        {
            var left = ie.TryGetProperty("left", out var l) ? l.GetString() ?? "" : "";
            return new IsEmptyCondition { Left = left };
        }
        if (el.TryGetProperty("isNotEmpty", out var ine))
        {
            var left = ine.TryGetProperty("left", out var l) ? l.GetString() ?? "" : "";
            return new IsNotEmptyCondition { Left = left };
        }
        if (el.TryGetProperty("not", out var notEl))
            return new NotCondition { Condition = ParseCondition(notEl) };

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
        static object BinObj(string left, string right, bool? ignoreCase) =>
            ignoreCase.HasValue ? new { left, right, ignoreCase = ignoreCase.Value } : new { left, right };

        return condition switch
        {
            EqualsCondition eq => new { equals = BinObj(eq.Left, eq.Right, eq.IgnoreCase) },
            NotEqualsCondition ne => new { notEquals = BinObj(ne.Left, ne.Right, ne.IgnoreCase) },
            ContainsCondition c => new { contains = BinObj(c.Left, c.Right, c.IgnoreCase) },
            GreaterThanCondition gt => new { greaterThan = BinObj(gt.Left, gt.Right, gt.IgnoreCase) },
            LessThanCondition lt => new { lessThan = BinObj(lt.Left, lt.Right, lt.IgnoreCase) },
            GreaterOrEqualCondition ge => new { greaterOrEqual = BinObj(ge.Left, ge.Right, ge.IgnoreCase) },
            LessOrEqualCondition le => new { lessOrEqual = BinObj(le.Left, le.Right, le.IgnoreCase) },
            StartsWithCondition sw => new { startsWith = BinObj(sw.Left, sw.Right, sw.IgnoreCase) },
            EndsWithCondition ew => new { endsWith = BinObj(ew.Left, ew.Right, ew.IgnoreCase) },
            RegexMatchCondition rx => new { regex = BinObj(rx.Left, rx.Right, rx.IgnoreCase) },
            InListCondition il => new { inList = BinObj(il.Left, il.Right, il.IgnoreCase) },
            IsEmptyCondition ie => new { isEmpty = new { left = ie.Left } },
            IsNotEmptyCondition ine => new { isNotEmpty = new { left = ine.Left } },
            NotCondition n => new { not = SerializeCondition(n.Condition) },
            AndCondition and => new { and = and.Conditions.Select(SerializeCondition) },
            OrCondition or => new { or = or.Conditions.Select(SerializeCondition) },
            _ => new { }
        };
    }
}

public record NodeLayout(Guid NodeId, double X, double Y);