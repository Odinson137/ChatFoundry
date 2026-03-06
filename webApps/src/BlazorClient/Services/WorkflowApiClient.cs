using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class WorkflowApiClient(HttpClient http) : IWorkflowApiClient
{
    // ─── Channels ─────────────────────────────────────────────────────────────

    public async Task<List<ChannelDto>> GetChannelsAsync()
    {
        var query = """
                query GetChannels {
                    channels(order: [{ createdAt: DESC }]) {
                        nodes {
                            id
                            name
                            maskedToken
                            channelType
                        }
                    }
                }
                """;

        var result = await ExecuteGraphQl<ChannelsConnectionResponse>(query);
        return result.Channels.Nodes;
    }

    public async Task<ChannelDto> AddChannelAsync(string name, string token, string channelType)
    {
        var query = """
                mutation AddChannel($input: AddChannelInput!) {
                    addChannel(input: $input) {
                        channel { id name maskedToken channelType }
                    }
                }
                """;

        var variables = new { input = new { name, token, channelType } };
        var result = await ExecuteGraphQl<AddChannelResponse>(query, variables);
        return result.AddChannel.Channel;
    }

    public async Task<ChannelDto> UpdateChannelAsync(Guid channelId, string name, string? token, string channelType)
    {
        var query = """
                mutation UpdateChannel($input: UpdateChannelInput!) {
                    updateChannel(input: $input) {
                        channel { id name maskedToken channelType }
                    }
                }
                """;

        var variables = new { input = new { channelId, name, token, channelType } };
        var result = await ExecuteGraphQl<UpdateChannelResponse>(query, variables);
        return result.UpdateChannel.Channel ?? throw new InvalidOperationException("Channel not found");
    }

    public async Task DeleteChannelAsync(Guid channelId)
    {
        var query = """
                mutation DeleteChannel($input: DeleteChannelInput!) {
                    deleteChannel(input: $input) {
                        channel { id }
                        error
                    }
                }
                """;

        var variables = new { input = new { channelId } };
        var result = await ExecuteGraphQl<DeleteChannelResponse>(query, variables);
        var payload = result.DeleteChannel;
        if (!string.IsNullOrEmpty(payload.Error))
            throw new InvalidOperationException(payload.Error);
    }

    // ─── Bots ────────────────────────────────────────────────────────────────

    public async Task<List<BotDto>> GetBotsAsync()
    {
        var query = """
                query GetBots {
                    bots(order: [{ createdAt: DESC }]) {
                        nodes {
                            id
                            name
                            createdAt
                            modifiedAt
                            workflows {
                                id
                                version
                                isActiveBotWorkflow
                            }
                            botChannels {
                                channelId
                                channel { id name }
                            }
                        }
                    }
                }
                """;

        var result = await ExecuteGraphQl<BotsConnectionResponse>(query);
        return result.Bots.Nodes;
    }

    public async Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId)
    {
        var query = """
                query GetBot($id: UUID!) {
                    bots(where: { id: { eq: $id } }) {
                        nodes {
                            id
                            name
                            createdAt
                            modifiedAt
                            workflows {
                                id
                                version
                                isActiveBotWorkflow
                                createdAt
                            }
                            botChannels {
                                channelId
                                channel { id name }
                            }
                        }
                    }
                }
                """;

        var variables = new { id = botId };
        var result = await ExecuteGraphQl<BotsConnectionResponse>(query, variables);
        return result.Bots.Nodes.FirstOrDefault();
    }

    public async Task<BotDto> AddBotAsync(string name, IReadOnlyList<Guid>? channelIds = null)
    {
        var query = """
                mutation Add($input: AddBotInput!) {
                    addBot(input: $input) {
                        bot { id name }
                    }
                }
                """;

        var channelIdsArray = channelIds?.ToArray() ?? Array.Empty<Guid>();
        var variables = new { input = new { name, channelIds = channelIdsArray } };
        var result = await ExecuteGraphQl<AddBotResponse>(query, variables);
        return result.AddBot.Bot ?? throw new InvalidOperationException("AddBot returned no bot");
    }

    public async Task<BotDto> UpdateBotAsync(Guid botId, string name, IReadOnlyList<Guid>? channelIds = null)
    {
        var query = """
                mutation Update($input: UpdateBotInput!) {
                    updateBot(input: $input) {
                        bot { id name }
                    }
                }
                """;

        var channelIdsArray = channelIds?.ToArray() ?? Array.Empty<Guid>();
        var variables = new { input = new { botId, name, channelIds = channelIdsArray } };
        var result = await ExecuteGraphQl<UpdateBotResponse>(query, variables);
        return result.UpdateBot.Bot ?? throw new InvalidOperationException("Bot not found");
    }

    public async Task DeleteBotAsync(Guid botId)
    {
        var query = """
                mutation Delete($input: DeleteBotInput!) {
                    deleteBot(input: $input) {
                        bot { id }
                    }
                }
                """;

        var variables = new { input = new { botId } };
        await ExecuteGraphQl<object>(query, variables);
    }

    // ─── Sessions ─────────────────────────────────────────────────────────────

    public async Task<List<SessionDto>> GetSessionsAsync(string? statusFilter = null)
    {
        var where = statusFilter != null
            ? $", where: {{ status: {{ eq: {statusFilter} }} }}"
            : "";

        var query = $$"""
                query GetSessions {
                    sessions(order: [{ createdAt: DESC }]{{where}}) {
                        nodes {
                            id
                            clientId
                            channel
                            channelId
                            workflowId
                            currentNodeId
                            status
                            createdAt
                            completedAt
                            workflow {
                                id
                                version
                                bot { id name }
                            }
                            actions {
                                id
                                nodeId
                                status
                                workflowNodeType
                                createdAt
                            }
                        }
                    }
                }
                """;

        var result = await ExecuteGraphQl<SessionsConnectionResponse>(query);
        return result.Sessions.Nodes;
    }

    public async Task<SessionsPageResult> GetSessionsPagedAsync(int first, string? after = null, string? statusFilter = null)
    {
        var where = statusFilter != null
            ? $", where: {{ status: {{ eq: {statusFilter} }} }}"
            : "";
        var afterArg = after != null ? ", $after: String" : "";
        var query = $$"""
                query GetSessionsPaged($first: Int!{{afterArg}}) {
                    sessions(first: $first{{(after != null ? ", after: $after" : "")}}, order: [{ createdAt: DESC }]{{where}}) {
                        totalCount
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            endCursor
                            startCursor
                        }
                        nodes {
                            id
                            clientId
                            channel
                            channelId
                            workflowId
                            currentNodeId
                            status
                            createdAt
                            completedAt
                            workflow {
                                id
                                version
                                bot { id name }
                            }
                            actions {
                                id
                                nodeId
                                status
                                workflowNodeType
                                createdAt
                            }
                        }
                    }
                }
                """;

        var variables = new Dictionary<string, object?> { ["first"] = first };
        if (after != null) variables["after"] = after;

        var result = await ExecuteGraphQl<SessionsConnectionResponse>(query, variables);
        var conn = result.Sessions;
        return new SessionsPageResult
        {
            Items = conn.Nodes,
            TotalCount = conn.TotalCount,
            HasNextPage = conn.PageInfo?.HasNextPage ?? false,
            HasPreviousPage = conn.PageInfo?.HasPreviousPage ?? false,
            EndCursor = conn.PageInfo?.EndCursor,
            StartCursor = conn.PageInfo?.StartCursor
        };
    }

    public async Task<SessionDto?> GetSessionByIdAsync(Guid sessionId)
    {
        var query = """
                query GetSession($id: UUID!) {
                    sessions(where: { id: { eq: $id } }) {
                        nodes {
                            id
                            clientId
                            channel
                            channelId
                            workflowId
                            currentNodeId
                            status
                            createdAt
                            completedAt
                            variables { key value }
                            workflow {
                                id
                                version
                                nodesDefinition
                                edgesDefinition
                                layoutDefinition
                                bot { id name }
                            }
                            actions {
                                id
                                nodeId
                                status
                                workflowNodeType
                                createdAt
                            }
                        }
                    }
                }
                """;

        var variables = new { id = sessionId };
        var result = await ExecuteGraphQl<SessionsConnectionResponse>(query, variables);
        return result.Sessions.Nodes.FirstOrDefault();
    }

    // ─── Workflows ───────────────────────────────────────────────────────────

    public async Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id)
    {
        var query = """
                query GetWorkflow($id: UUID!) {
                    workflows(where: { id: { eq: $id } }) {
                        nodes {
                            id
                            botId
                            nodesDefinition
                            edgesDefinition
                            layoutDefinition
                            version
                            isActiveBotWorkflow
                            inputParametersDefinition
                            outputParametersDefinition
                        }
                    }
                }
                """;

        var variables = new { id };
        var result = await ExecuteGraphQl<WorkflowsConnectionResponse>(query, variables);
        return result.Workflows.Nodes.FirstOrDefault();
    }

    public async Task<List<WorkflowListItem>> GetWorkflowsListAsync()
    {
        var page = await GetWorkflowsPageAsync(first: 10, after: null);
        return page.Items;
    }

    public async Task<WorkflowListPage> GetWorkflowsPageAsync(int first = 10, string? after = null, int? last = null, string? before = null)
    {
        var query = """
                query GetWorkflowsPage($first: Int, $after: String, $last: Int, $before: String) {
                    workflows(first: $first, after: $after, last: $last, before: $before) {
                        nodes {
                            id
                            version
                            bot { id name }
                            inputParametersDefinition
                            outputParametersDefinition
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            endCursor
                            startCursor
                        }
                    }
                }
                """;

        var variables = new { first, after, last, before };
        var result = await ExecuteGraphQl<WorkflowsPageResponse>(query, variables);
        var conn = result.Workflows ?? new WorkflowListConnectionWithPageInfo();
        var nodes = conn.Nodes ?? [];
        return new WorkflowListPage
        {
            Items = nodes.Select(n => new WorkflowListItem
            {
                Id = n.Id,
                Version = n.Version,
                Bot = n.Bot,
                InputParameters = DeserializeParameters(n.InputParametersDefinition),
                OutputParameters = DeserializeParameters(n.OutputParametersDefinition)
            }).ToList(),
            HasNextPage = conn.PageInfo?.HasNextPage ?? false,
            HasPreviousPage = conn.PageInfo?.HasPreviousPage ?? false,
            EndCursor = conn.PageInfo?.EndCursor,
            StartCursor = conn.PageInfo?.StartCursor
        };
    }

    private static List<WorkflowParameterDto> DeserializeParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]" || json.Trim() == "{}")
            return [];
        try
        {
            var list = JsonSerializer.Deserialize<List<WorkflowParameterDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AddBotWorkflowAsync(Guid botId, int version)
    {
        var query = """
                mutation Add($input: AddBotWorkflowInput!) {
                    addBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var variables = new
        {
            input = new
            {
                botId,
                version,
                nodesDefinition = "[]",
                edgesDefinition = "[]",
                layoutDefinition = "[]",
                isActiveBotWorkflow = false
            }
        };

        try { await ExecuteGraphQl<object>(query, variables); return true; }
        catch { return false; }
    }

    public async Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout, List<WorkflowParameterDto>? inputParameters = null, List<WorkflowParameterDto>? outputParameters = null)
    {
        var query = """
                mutation UpdateWorkflowDefs($input: UpdateBotWorkflowInput!) {
                    updateBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var inputParametersDefinition = SerializeParameters(inputParameters);
        var outputParametersDefinition = SerializeParameters(outputParameters);

        var variables = new
        {
            input = new
            {
                workflowId,
                nodesDefinition = nodes,
                edgesDefinition = edges,
                layoutDefinition = layout,
                inputParametersDefinition,
                outputParametersDefinition
            }
        };

        try { await ExecuteGraphQl<object>(query, variables); return true; }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update workflow definitions: {ex.Message}");
            return false;
        }
    }

    private static string SerializeParameters(List<WorkflowParameterDto>? list)
    {
        if (list == null || list.Count == 0) return "[]";
        return JsonSerializer.Serialize(list, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public async Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive)
    {
        var query = """
                mutation Update($input: UpdateBotWorkflowInput!) {
                    updateBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var variables = new { input = new { workflowId, isActiveBotWorkflow = isActive } };

        try { await ExecuteGraphQl<object>(query, variables); return true; }
        catch { return false; }
    }

    public async Task<bool> DeleteBotWorkflowAsync(Guid workflowId)
    {
        var query = """
                mutation Delete($input: DeleteBotWorkflowInput!) {
                    deleteBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var variables = new { input = new { workflowId } };

        try { await ExecuteGraphQl<object>(query, variables); return true; }
        catch { return false; }
    }

    // ─── Shared ──────────────────────────────────────────────────────────────

    private async Task<T> ExecuteGraphQl<T>(string query, object? variables = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/workflow/graphql");
        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request);
        var jsonString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Http Error {response.StatusCode}: {jsonString}");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);

        return gqlResponse!.Data!;
    }
    
    public async Task RefreshChannelWebhookAsync(Guid channelId)
    {
        var query = """
                    mutation RefreshChannelWebhook($input: RefreshChannelWebhookInput!) {
                        refreshChannelWebhook(input: $input) {
                            channel { id name }
                        }
                    }
                    """;

        var variables = new { input = new { channelId } };
        await ExecuteGraphQl<object>(query, variables);
    }

    // ─── Response DTOs (private, для десериализацииConnection-обёрток) ──────

    private class BotsConnectionResponse
    {
        public BotConnection Bots { get; set; } = new();
    }

    private class BotConnection
    {
        public List<BotDto> Nodes { get; set; } = [];
    }

    private class WorkflowsConnectionResponse
    {
        public WorkflowConnection Workflows { get; set; } = new();
    }

    private class WorkflowConnection
    {
        public List<WorkflowResponse> Nodes { get; set; } = [];
    }

    private class WorkflowsListConnectionResponse
    {
        public WorkflowListConnection? Workflows { get; set; }
    }

    private class WorkflowsPageResponse
    {
        public WorkflowListConnectionWithPageInfo? Workflows { get; set; }
    }

    private class WorkflowListConnectionWithPageInfo
    {
        public List<WorkflowListNode> Nodes { get; set; } = [];
        public WorkflowPageInfoDto? PageInfo { get; set; }
    }

    private class WorkflowPageInfoDto
    {
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? EndCursor { get; set; }
        public string? StartCursor { get; set; }
    }

    private class WorkflowListConnection
    {
        public List<WorkflowListNode> Nodes { get; set; } = [];
    }

    private class WorkflowListNode
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public WorkflowListItemBot? Bot { get; set; }
        public string? InputParametersDefinition { get; set; }
        public string? OutputParametersDefinition { get; set; }
    }

    private class WorkflowListItemsResponse
    {
        public List<WorkflowListItem> WorkflowListItems { get; set; } = [];
    }

    private class AddBotResponse
    {
        public BotMutationPayload AddBot { get; set; } = new();
    }

    private class UpdateBotResponse
    {
        public BotMutationPayload UpdateBot { get; set; } = new();
    }

    private class BotMutationPayload
    {
        public BotDto? Bot { get; set; }
    }

    private class ChannelsConnectionResponse
    {
        public ChannelConnection Channels { get; set; } = new();
    }

    private class ChannelConnection
    {
        public List<ChannelDto> Nodes { get; set; } = [];
    }

    private class AddChannelResponse
    {
        public AddChannelPayload AddChannel { get; set; } = new();
    }

    private class AddChannelPayload
    {
        public ChannelDto Channel { get; set; } = new();
    }

    private class UpdateChannelResponse
    {
        public UpdateChannelPayload UpdateChannel { get; set; } = new();
    }

    private class UpdateChannelPayload
    {
        public ChannelDto? Channel { get; set; }
    }

    private class DeleteChannelResponse
    {
        public DeleteChannelPayload DeleteChannel { get; set; } = new();
    }

    private class DeleteChannelPayload
    {
        public ChannelDto? Channel { get; set; }
        public string? Error { get; set; }
    }

    private class SessionsConnectionResponse
    {
        public SessionConnection Sessions { get; set; } = new();
    }

    private class SessionConnection
    {
        public List<SessionDto> Nodes { get; set; } = [];
        public int TotalCount { get; set; }
        public SessionPageInfo? PageInfo { get; set; }
    }

    private class SessionPageInfo
    {
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? EndCursor { get; set; }
        public string? StartCursor { get; set; }
    }
}