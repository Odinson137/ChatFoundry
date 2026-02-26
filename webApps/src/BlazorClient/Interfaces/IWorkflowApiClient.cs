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
    Task<SessionDto?> GetSessionByIdAsync(Guid sessionId);

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