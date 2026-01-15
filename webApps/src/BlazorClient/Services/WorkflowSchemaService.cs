using System.Text.Json;
using BlazorClient.Interfaces;
using BlazorClient.Models;

namespace BlazorClient.Services;

public class WorkflowSchemaService : IWorkflowSchemaService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public WorkflowSchema Deserialize(string nodes, string edges, string layout)
    {
        return new WorkflowSchema(
            JsonSerializer.Deserialize<List<NodeDefinition>>(nodes, _options) ?? [],
            JsonSerializer.Deserialize<List<EdgeDefinition>>(edges, _options) ?? [],
            JsonSerializer.Deserialize<List<LayoutDefinition>>(layout, _options) ?? []
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