using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IWorkflowApiClient
{
    // ─── Bots ────────────────────────────────────────────────────────────────
    Task<List<BotDto>> GetBotsAsync();
    Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId);
    Task<BotDto> AddBotAsync(string name, string token);
    Task<BotDto> UpdateBotAsync(Guid botId, string name);
    Task DeleteBotAsync(Guid botId);
    Task RefreshBotWebhookAsync(Guid botId);

    // ─── Workflows ───────────────────────────────────────────────────────────
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
    Task<bool> AddBotWorkflowAsync(Guid botId, int version);
    Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout);
    Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive);
    Task<bool> DeleteBotWorkflowAsync(Guid workflowId);
}

public record GqlData(GqlWorkflowContent Data);
public record GqlWorkflowContent(List<WorkflowResponse> Workflows);
public record WorkflowResponse(
    Guid Id,
    string NodesDefinition,
    string EdgesDefinition,
    string LayoutDefinition);