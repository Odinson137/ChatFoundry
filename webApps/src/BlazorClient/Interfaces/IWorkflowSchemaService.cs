using BlazorClient.Models;

namespace BlazorClient.Interfaces;

public interface IWorkflowSchemaService
{
    WorkflowSchema Deserialize(string nodes, string edges, string layout);
    (string Nodes, string Edges, string Layout) Serialize(WorkflowSchema schema);
}