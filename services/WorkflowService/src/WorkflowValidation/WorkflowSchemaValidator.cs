using System.Text.Json;

namespace WorkflowValidation;

/// <summary>
/// Проверка JSON схемы workflow (формат экспорта редактора: nodes, edges, layout).
/// </summary>
public static class WorkflowSchemaValidator
{
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Start", "Message", "Ask", "Input", "Condition", "Wait", "Media", "SetAttribute",
        "HttpRequest", "AIFilter", "AIGenerate", "SubWorkflow", "Command",
        "Image", "Video", "Audio", "Voice", "File", "Sticker", "Link"
    };

    private static readonly HashSet<string> DisallowedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "End", "SetVariable"
    };

    public static IReadOnlyList<string> Validate(string json)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("JSON пуст.");
            return errors;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add($"Некорректный JSON: {ex.Message}");
            return errors;
        }

        using (doc)
        {
            return Validate(doc.RootElement, errors);
        }
    }

    public static IReadOnlyList<string> Validate(JsonElement root, List<string>? errors = null)
    {
        errors ??= [];
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("Корень JSON должен быть объектом.");
            return errors;
        }

        if (!root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add("Требуется массив \"nodes\".");
            return errors;
        }

        if (!root.TryGetProperty("edges", out var edgesEl) || edgesEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add("Требуется массив \"edges\".");
            return errors;
        }

        if (!root.TryGetProperty("layout", out var layoutEl) || layoutEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add("Требуется массив \"layout\".");
            return errors;
        }

        var nodeIds = new Dictionary<Guid, string>(); // id -> type
        var startCount = 0;

        foreach (var node in nodesEl.EnumerateArray())
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Каждый элемент nodes должен быть объектом.");
                continue;
            }

            if (!node.TryGetProperty("id", out var idProp))
            {
                errors.Add("У узла отсутствует id.");
                continue;
            }

            if (!TryGetGuid(idProp, out var nodeId))
            {
                errors.Add("Некорректный guid у узла (id).");
                continue;
            }

            if (nodeIds.ContainsKey(nodeId))
            {
                errors.Add($"Дублирующийся id узла: {nodeId}.");
                continue;
            }

            var typeStr = node.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString() ?? ""
                : "";

            if (string.IsNullOrEmpty(typeStr))
            {
                errors.Add($"У узла {nodeId} пустой type.");
            }
            else
            {
                if (DisallowedNodeTypes.Contains(typeStr))
                    errors.Add($"Тип узла не поддерживается: {typeStr} (устаревший или удалённый).");
                else if (!AllowedNodeTypes.Contains(typeStr))
                    errors.Add($"Неизвестный тип узла: {typeStr}.");
            }

            if (string.Equals(typeStr, "Start", StringComparison.OrdinalIgnoreCase))
                startCount++;

            nodeIds[nodeId] = typeStr;
        }

        if (startCount != 1)
            errors.Add(startCount == 0 ? "Должен быть ровно один узел Start." : "Должен быть ровно один узел Start (найдено: " + startCount + ").");

        foreach (var lay in layoutEl.EnumerateArray())
        {
            if (lay.ValueKind != JsonValueKind.Object)
                continue;
            if (lay.TryGetProperty("nodeId", out var nid) && TryGetGuid(nid, out var lid) && !nodeIds.ContainsKey(lid))
                errors.Add($"layout ссылается на неизвестный nodeId: {lid}.");
        }

        Guid? startNodeId = null;
        foreach (var kv in nodeIds)
        {
            if (string.Equals(kv.Value, "Start", StringComparison.OrdinalIgnoreCase))
            {
                startNodeId = kv.Key;
                break;
            }
        }

        foreach (var edge in edgesEl.EnumerateArray())
        {
            if (edge.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Каждый элемент edges должен быть объектом.");
                continue;
            }

            if (!edge.TryGetProperty("from", out var fromEl) || !TryGetGuid(fromEl, out var fromId))
            {
                errors.Add("Ребро: некорректное поле from.");
                continue;
            }

            if (!edge.TryGetProperty("to", out var toEl) || !TryGetGuid(toEl, out var toId))
            {
                errors.Add("Ребро: некорректное поле to.");
                continue;
            }

            if (!nodeIds.ContainsKey(fromId))
                errors.Add($"Ребро: from {fromId} не существует среди узлов.");
            if (!nodeIds.ContainsKey(toId))
                errors.Add($"Ребро: to {toId} не существует среди узлов.");
        }

        if (startCount == 1 && startNodeId.HasValue && nodeIds.Count > 0)
        {
            var reachable = new HashSet<Guid>();
            var adj = new Dictionary<Guid, List<Guid>>();
            foreach (var edge in edgesEl.EnumerateArray())
            {
                if (edge.ValueKind != JsonValueKind.Object) continue;
                if (!edge.TryGetProperty("from", out var f) || !TryGetGuid(f, out var fa)) continue;
                if (!edge.TryGetProperty("to", out var t) || !TryGetGuid(t, out var ta)) continue;
                if (!adj.TryGetValue(fa, out var list))
                {
                    list = [];
                    adj[fa] = list;
                }
                list.Add(ta);
            }

            var q = new Queue<Guid>();
            q.Enqueue(startNodeId.Value);
            reachable.Add(startNodeId.Value);
            while (q.Count > 0)
            {
                var u = q.Dequeue();
                if (!adj.TryGetValue(u, out var outs)) continue;
                foreach (var v in outs)
                {
                    if (reachable.Add(v))
                        q.Enqueue(v);
                }
            }

            foreach (var id in nodeIds.Keys)
            {
                if (!reachable.Contains(id))
                    errors.Add($"Узел {id} недостижим из Start (нет пути по рёбрам).");
            }
        }

        return errors;
    }

    private static bool TryGetGuid(JsonElement el, out Guid guid)
    {
        guid = default;
        if (el.ValueKind == JsonValueKind.String)
            return Guid.TryParse(el.GetString(), out guid);
        return el.TryGetGuid(out guid);
    }
}
