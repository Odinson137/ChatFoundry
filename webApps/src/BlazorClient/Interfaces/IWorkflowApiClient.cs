using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IWorkflowApiClient
{
    // ─── Channels ───────────────────────────────────────────────────────────
    Task<List<ChannelDto>> GetChannelsAsync();
    Task<ChannelDto> AddChannelAsync(string name, string token, string channelType);
    Task<ChannelDto> UpdateChannelAsync(Guid channelId, string name, string? token, string channelType);
    /// <summary>Удаляет канал. Если бэкенд вернул error (канал привязан к ботам), выбрасывает исключение с текстом ошибки.</summary>
    Task DeleteChannelAsync(Guid channelId);
    /// <summary>Актуализирует webhook для канала (Telegram и др.).</summary>
    Task RefreshChannelWebhookAsync(Guid channelId);

    // ─── Bots ────────────────────────────────────────────────────────────────
    Task<List<BotDto>> GetBotsAsync();
    Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId);
    Task<BotDto> AddBotAsync(string name, IReadOnlyList<Guid>? channelIds = null);
    Task<BotDto> UpdateBotAsync(Guid botId, string name, IReadOnlyList<Guid>? channelIds = null);
    Task DeleteBotAsync(Guid botId);

    // ─── Sessions ────────────────────────────────────────────────────────────
    Task<List<SessionDto>> GetSessionsAsync(string? statusFilter = null);
    /// <summary>Постраничная загрузка сессий (cursor-based).</summary>
    Task<SessionsPageResult> GetSessionsPagedAsync(int first, string? after = null, SessionListFilter? filter = null);
    Task<SessionDto?> GetSessionByIdAsync(Guid sessionId);

    // ─── Workflows ───────────────────────────────────────────────────────────
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
    Task<List<WorkflowListItem>> GetWorkflowsListAsync();
    /// <summary>Постраничная загрузка процессов для модального выбора (first: 10, cursor-based).</summary>
    Task<WorkflowListPage> GetWorkflowsPageAsync(int first = 10, string? after = null, int? last = null, string? before = null);
    Task<bool> AddBotWorkflowAsync(Guid botId, int version);
    /// <summary>Создаёт новую версию workflow как копию существующей (тот же бот, следующий номер версии).</summary>
    Task<bool> CopyBotWorkflowAsync(Guid sourceWorkflowId);
    Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout, List<WorkflowParameterDto>? inputParameters = null, List<WorkflowParameterDto>? outputParameters = null);
    Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive);
    Task<bool> DeleteBotWorkflowAsync(Guid workflowId);
}

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

/// <summary>Одна страница списка процессов (для модального выбора с пагинацией).</summary>
public class WorkflowListPage
{
    public List<WorkflowListItem> Items { get; set; } = [];
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? EndCursor { get; set; }
    public string? StartCursor { get; set; }
}

/// <summary>Фильтр списка сессий: поиск по clientId, бот, статус, период создания/завершения.</summary>
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

/// <summary>Одна страница списка сессий (cursor-based пагинация).</summary>
public class SessionsPageResult
{
    public List<SessionDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? EndCursor { get; set; }
    public string? StartCursor { get; set; }
}