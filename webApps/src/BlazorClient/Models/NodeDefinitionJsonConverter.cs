using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorClient.Models;

public sealed class NodeDefinitionJsonConverter : JsonConverter<NodeDefinition>
{
    public override NodeDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!TryGetGuid(root, out var id))
            throw new JsonException("Node \"id\" is required.");

        var typeStr = GetStringProperty(root, "type") ?? "";
        var label = GetStringProperty(root, "label") ?? "";

        NodeData? data = null;
        if (TryGetProperty(root, "data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
            data = DeserializeNodeData(dataEl, typeStr, options);

        return new NodeDefinition(id, typeStr, label, data);
    }

    public override void Write(Utf8JsonWriter writer, NodeDefinition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("type", value.Type);
        writer.WriteString("label", value.Label);
        writer.WritePropertyName("data");
        if (value.Data == null)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, value.Data, value.Data.GetType(), options);
        writer.WriteEndObject();
    }

    private static NodeData? DeserializeNodeData(JsonElement dataEl, string workflowNodeType, JsonSerializerOptions options)
    {
        if (dataEl.ValueKind == JsonValueKind.Object && PropertyExists(dataEl, "$type"))
            return JsonSerializer.Deserialize<NodeData>(dataEl.GetRawText(), options);

        var concrete = MapWorkflowNodeTypeToDataType(workflowNodeType);
        return (NodeData?)JsonSerializer.Deserialize(dataEl.GetRawText(), concrete, options);
    }

    private static Type MapWorkflowNodeTypeToDataType(string workflowNodeType)
    {
        return workflowNodeType.Trim().ToLowerInvariant() switch
        {
            "message" => typeof(MessageNodeData),
            "ask" => typeof(AskNodeData),
            "setattribute" => typeof(SetAttributeNodeData),
            "setvariable" => typeof(SetVariableNodeData),
            "httprequest" => typeof(HttpRequestNodeData),
            "aigenerate" => typeof(AIGenerateNodeData),
            "media" => typeof(MediaNodeData),
            "subworkflow" => typeof(SubWorkflowNodeData),
            "transfertooperator" => typeof(EmptyNodeData),
            _ => typeof(EmptyNodeData)
        };
    }

    private static bool TryGetGuid(JsonElement root, out Guid id)
    {
        if (!TryGetProperty(root, "id", out var el))
        {
            id = default;
            return false;
        }

        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out id))
            return true;

        id = default;
        return false;
    }

    private static string? GetStringProperty(JsonElement root, string name)
    {
        return TryGetProperty(root, name, out var el) ? el.GetString() : null;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool PropertyExists(JsonElement obj, string name)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
