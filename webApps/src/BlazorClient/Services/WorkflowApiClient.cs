using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class WorkflowApiClient(HttpClient http) : IWorkflowApiClient
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GetWorkflowAiInstructionMarkdownAsync(CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"{ApiEndpoints.Api}/workflow/public/workflow-ai/prompt", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<GenerateWorkflowFromAiResult> GenerateWorkflowFromAiAsync(string userPrompt, bool mergeMode,
        WorkflowSchema? currentWorkflow, CancellationToken cancellationToken = default)
    {
        var body = new GenerateWorkflowFromAiClientRequest
        {
            UserPrompt = userPrompt,
            Mode = mergeMode ? "merge" : "replace",
            CurrentWorkflow = mergeMode ? currentWorkflow : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/workflow/api/workflow-ai/generate")
        {
            Content = JsonContent.Create(body, options: WebJsonOptions)
        };

        var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var parsedErrors = TryExtractGenerateWorkflowErrors(json);
            if (parsedErrors.Count > 0)
                return new GenerateWorkflowFromAiResult(false, null, parsedErrors);

            return new GenerateWorkflowFromAiResult(false, null, [$"Server error {(int)response.StatusCode}. Please try again later."]);
        }

        var dto = JsonSerializer.Deserialize<GenerateWorkflowFromAiResponseDto>(json, WebJsonOptions);
        if (dto == null)
            return new GenerateWorkflowFromAiResult(false, null, ["Empty server response."]);

        return new GenerateWorkflowFromAiResult(dto.Success, dto.WorkflowJson, dto.Errors ?? []);
    }

    private static List<string> TryExtractGenerateWorkflowErrors(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var dto = JsonSerializer.Deserialize<GenerateWorkflowFromAiResponseDto>(json, WebJsonOptions);
            return dto?.Errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private sealed class GenerateWorkflowFromAiClientRequest
    {
        public string UserPrompt { get; set; } = "";
        public string Mode { get; set; } = "replace";
        public WorkflowSchema? CurrentWorkflow { get; set; }
    }

    private sealed class GenerateWorkflowFromAiResponseDto
    {
        public bool Success { get; set; }
        public string? WorkflowJson { get; set; }
        public List<string>? Errors { get; set; }
    }

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
                                modifiedAt
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

    public async Task<SessionsPageResult> GetSessionsPagedAsync(int first, string? after = null, SessionListFilter? filter = null)
    {
        var query = """
                query GetSessionsPaged($first: Int!, $after: String, $where: SessionFilterInput) {
                    sessions(first: $first, after: $after, where: $where, order: [{ createdAt: DESC }]) {
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
                                errorMessage
                            }
                        }
                    }
                }
                """;

        var variables = new Dictionary<string, object?>
        {
            ["first"] = first,
            ["after"] = after,
            ["where"] = BuildSessionsWhere(filter)
        };

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

    private static object? BuildSessionsWhere(SessionListFilter? filter)
    {
        if (filter == null) return null;

        var conditions = new List<object>();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add(new { status = new { eq = filter.Status.Trim().ToUpperInvariant() } });

        if (!string.IsNullOrWhiteSpace(filter.Search))
            conditions.Add(new { clientId = new { contains = filter.Search.Trim() } });

        if (filter.BotId.HasValue)
            conditions.Add(new { workflow = new { botId = new { eq = filter.BotId.Value } } });

        if (filter.CreatedFrom.HasValue || filter.CreatedTo.HasValue)
        {
            var createdAt = new Dictionary<string, object?>();
            if (filter.CreatedFrom.HasValue) createdAt["gte"] = filter.CreatedFrom.Value;
            if (filter.CreatedTo.HasValue) createdAt["lte"] = filter.CreatedTo.Value;
            conditions.Add(new Dictionary<string, object> { ["createdAt"] = createdAt });
        }

        if (filter.CompletedFrom.HasValue || filter.CompletedTo.HasValue)
        {
            var completedAt = new Dictionary<string, object?>();
            if (filter.CompletedFrom.HasValue) completedAt["gte"] = filter.CompletedFrom.Value;
            if (filter.CompletedTo.HasValue) completedAt["lte"] = filter.CompletedTo.Value;
            conditions.Add(new Dictionary<string, object> { ["completedAt"] = completedAt });
        }

        if (conditions.Count == 0) return null;
        if (conditions.Count == 1) return conditions[0];
        return new { and = conditions };
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
                                errorMessage
                            }
                        }
                    }
                }
                """;

        var variables = new { id = sessionId };
        var result = await ExecuteGraphQl<SessionsConnectionResponse>(query, variables);
        return result.Sessions.Nodes.FirstOrDefault();
    }

    public async Task<bool> CompleteSessionAsync(Guid sessionId)
    {
        var query = """
                mutation CompleteSession($input: CompleteSessionInput!) {
                    completeSession(input: $input) {
                        session { id status }
                        error
                    }
                }
                """;

        var variables = new { input = new { sessionId } };
        try
        {
            var result = await ExecuteGraphQl<CompleteSessionResponse>(query, variables);
            return result.CompleteSession.Session != null && string.IsNullOrEmpty(result.CompleteSession.Error);
        }
        catch
        {
            return false;
        }
    }

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

    public async Task<bool> CopyBotWorkflowAsync(Guid sourceWorkflowId)
    {
        var query = """
                mutation Copy($input: CopyBotWorkflowInput!) {
                    copyBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var variables = new { input = new { sourceWorkflowId } };

        try
        {
            await ExecuteGraphQl<object>(query, variables);
            return true;
        }
        catch
        {
            return false;
        }
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
        if (gqlResponse == null)
            throw new InvalidOperationException("Server returned an empty response.");

        var firstGraphQlError = gqlResponse.Errors?
            .Select(e => e.Message)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        if (!string.IsNullOrWhiteSpace(firstGraphQlError))
            throw new InvalidOperationException(firstGraphQlError);

        if (gqlResponse.Data == null)
            throw new InvalidOperationException("Failed to process server response.");

        return gqlResponse.Data;
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

    private class CompleteSessionResponse
    {
        public CompleteSessionPayload CompleteSession { get; set; } = new();
    }

    private class CompleteSessionPayload
    {
        public SessionDto? Session { get; set; }
        public string? Error { get; set; }
    }
}