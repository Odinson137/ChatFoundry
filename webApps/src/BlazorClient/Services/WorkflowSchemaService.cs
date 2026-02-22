using System.Text.Json;
using BlazorClient.Interfaces;
using BlazorClient.Models; // Убедитесь, что эта using-директива присутствует
using System.Collections.Generic;

namespace BlazorClient.Services;

public class WorkflowSchemaService : IWorkflowSchemaService
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // Дискриминатор $type может быть не в начале объекта (например: {"text":"...","$type":"Message"})
        AllowOutOfOrderMetadataProperties = true
    };

    public WorkflowSchema Deserialize(string? nodes, string? edges, string? layout)
    {
        return new WorkflowSchema(
            JsonSerializer.Deserialize<List<NodeDefinition>>(string.IsNullOrWhiteSpace(nodes) ? "[]" : nodes, _options) ?? [],
            JsonSerializer.Deserialize<List<EdgeDefinition>>(string.IsNullOrWhiteSpace(edges) ? "[]" : edges, _options) ?? [],
            JsonSerializer.Deserialize<List<LayoutDefinition>>(string.IsNullOrWhiteSpace(layout) ? "[]" : layout, _options) ?? []
        );
    }

    public (string Nodes, string Edges, string Layout) Serialize(WorkflowSchema schema)
    {
        return (
            JsonSerializer.Serialize(schema.Nodes, _options),
            JsonSerializer.Serialize(schema.Edges, _options),
            JsonSerializer.Serialize(schema.Layout, _options)
        );
    }
}