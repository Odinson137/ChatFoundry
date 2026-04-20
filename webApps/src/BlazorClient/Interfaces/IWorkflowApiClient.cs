using BlazorClient.Models;
using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IWorkflowApiClient
{
    
    Task<List<ChannelDto>> GetChannelsAsync();
    Task<ChannelDto> AddChannelAsync(string name, string token, string channelType);
    Task<ChannelDto> UpdateChannelAsync(Guid channelId, string name, string? token, string channelType);
    Task DeleteChannelAsync(Guid channelId);
    Task RefreshChannelWebhookAsync(Guid channelId);

    Task<List<BotDto>> GetBotsAsync();
    Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId);
    Task<BotDto> AddBotAsync(string name, IReadOnlyList<Guid>? channelIds = null);
    Task<BotDto> UpdateBotAsync(Guid botId, string name, IReadOnlyList<Guid>? channelIds = null);
    Task DeleteBotAsync(Guid botId);

    
    Task<List<SessionDto>> GetSessionsAsync(string? statusFilter = null);
    /// <summary>Постраничная загрузка сессий (cursor-based).</summary>
    Task<SessionsPageResult> GetSessionsPagedAsync(int first, string? after = null, SessionListFilter? filter = null);
    Task<SessionDto?> GetSessionByIdAsync(Guid sessionId);

    
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
    Task<List<WorkflowListItem>> GetWorkflowsListAsync();
    Task<WorkflowListPage> GetWorkflowsPageAsync(int first = 10, string? after = null, int? last = null, string? before = null);
    Task<bool> AddBotWorkflowAsync(Guid botId, int version);
    Task<bool> CopyBotWorkflowAsync(Guid sourceWorkflowId);
    Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout, List<WorkflowParameterDto>? inputParameters = null, List<WorkflowParameterDto>? outputParameters = null);
    Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive);
    Task<bool> DeleteBotWorkflowAsync(Guid workflowId);

    Task<string> GetWorkflowAiInstructionMarkdownAsync(CancellationToken cancellationToken = default);

    Task<GenerateWorkflowFromAiResult> GenerateWorkflowFromAiAsync(string userPrompt, bool mergeMode, WorkflowSchema? currentWorkflow, CancellationToken cancellationToken = default);
}

public record GenerateWorkflowFromAiResult(bool Success, string? WorkflowJson, IReadOnlyList<string> Errors);

public record GqlData(GqlWorkflowContent Data);
public record GqlWorkflowContent(List<WorkflowResponse> Workflows);
public record WorkflowResponse(
    Guid Id,
    string NodesDefinition,
    string EdgesDefinition,
    string LayoutDefinition,
    string? InputParametersDefinition = null,
    string? OutputParametersDefinition = null);

public class WorkflowParameterDto
{
    public string Name { get; set; } = "";
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}

public class WorkflowListItem
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public WorkflowListItemBot? Bot { get; set; }
    public List<WorkflowParameterDto> InputParameters { get; set; } = [];
    public List<WorkflowParameterDto> OutputParameters { get; set; } = [];
}

public class WorkflowListItemBot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class WorkflowListPage
{
    public List<WorkflowListItem> Items { get; set; } = [];
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? EndCursor { get; set; }
    public string? StartCursor { get; set; }
}

public class SessionListFilter
{
    public string? Search { get; set; }
    public Guid? BotId { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? CompletedFrom { get; set; }
    public DateTime? CompletedTo { get; set; }
}

public class SessionsPageResult
{
    public List<SessionDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? EndCursor { get; set; }
    public string? StartCursor { get; set; }
}