using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IWorkflowApiClient
{
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
    Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId);
    
    Task<bool> AddBotWorkflowAsync(Guid botId, int version);
    Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive);
    Task<bool> DeleteBotWorkflowAsync(Guid workflowId);
    Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nStr, string eStr, string lStr);
}

public record GqlData(GqlWorkflowContent Data);
public record GqlWorkflowContent(List<WorkflowResponse> Workflows);
public record WorkflowResponse(
    Guid Id,
    string NodesDefinition,
    string EdgesDefinition,
    string LayoutDefinition);