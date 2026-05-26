using System.Text.Json;
using BlazorClient.Interfaces;
using BlazorClient.Models;

namespace BlazorClient.Services;

public class WorkflowSchemaService : IWorkflowSchemaService
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,

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