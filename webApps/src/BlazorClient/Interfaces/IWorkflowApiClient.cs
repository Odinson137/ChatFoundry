namespace BlazorClient.Interfaces;

public interface IWorkflowApiClient
{
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
}

public record GqlData(GqlWorkflowContent Data);
public record GqlWorkflowContent(List<WorkflowResponse> Workflows);
public record WorkflowResponse(
    Guid Id,
    string NodesDefinition,
    string EdgesDefinition,
    string LayoutDefinition);