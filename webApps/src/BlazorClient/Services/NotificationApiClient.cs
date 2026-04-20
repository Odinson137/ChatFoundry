using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class NotificationApiClient(HttpClient http) : INotificationApiClient
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<LiveChatSessionDto>> GetLiveChatSessionsAsync(string? status = null)
    {
        var fieldSelection = """
                            id
                            workflowSessionId
                            externalUserId
                            channel
                            channelId
                            clientChannelId
                            botId
                            botName
                            companyId
                            clientFirstName
                            clientUserName
                            status
                            operatorId
                            takenAt
                            closedAt
                            lastMessagePreview
                            createdAt
            """;

        if (status != null)
        {
            var query = """
                    query GetLiveChatSessions($status: LiveChatSessionStatus) {
                        liveChatSessions(order: [{ createdAt: DESC }], where: { status: { eq: $status } }) {
                            nodes { __FIELDS__ }
                        }
                    }
                    """.Replace("__FIELDS__", fieldSelection);
            var variables = new { status = ToGraphQlEnumValue(status) };
            var result = await ExecuteGraphQl<LiveChatSessionsResponse>(query, variables);
            return result.LiveChatSessions.Nodes;
        }

        var queryAll = """
                query GetLiveChatSessions {
                    liveChatSessions(order: [{ createdAt: DESC }]) {
                        nodes { __FIELDS__ }
                    }
                }
                """.Replace("__FIELDS__", fieldSelection);

        var resultAll = await ExecuteGraphQl<LiveChatSessionsResponse>(queryAll);
        return resultAll.LiveChatSessions.Nodes;
    }

    private static string ToGraphQlEnumValue(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return status;

        var normalized = new System.Text.StringBuilder(status.Length + 4);
        for (var i = 0; i < status.Length; i++)
        {
            var current = status[i];
            if (i > 0 && char.IsUpper(current) && char.IsLetter(status[i - 1]))
                normalized.Append('_');

            normalized.Append(char.ToUpperInvariant(current));
        }

        return normalized.ToString();
    }

    public async Task<LiveChatSessionDto?> GetLiveChatSessionAsync(Guid id)
    {
        var query = """
                query GetLiveChatSession($id: UUID!) {
                    liveChatSession(id: $id) {
                        id
                        workflowSessionId
                        externalUserId
                        channel
                        channelId
                        clientChannelId
                        botId
                        botName
                        companyId
                        clientFirstName
                        clientUserName
                        status
                        operatorId
                        takenAt
                        closedAt
                        lastMessagePreview
                        createdAt
                    }
                }
                """;

        var variables = new { id };
        var result = await ExecuteGraphQl<LiveChatSessionSingleResponse>(query, variables);
        return result.LiveChatSession?.FirstOrDefault();
    }

    public async Task TakeLiveChatAsync(Guid liveChatSessionId)
    {
        var query = """
                mutation TakeLiveChat($id: UUID!) {
                    takeLiveChat(liveChatSessionId: $id) { id status operatorId }
                }
                """;

        var variables = new { id = liveChatSessionId };
        await ExecuteGraphQl<object>(query, variables);
    }

    public async Task SendLiveChatMessageAsync(Guid liveChatSessionId, string text)
    {
        var query = """
                mutation SendLiveChatMessage($id: UUID!, $text: String!) {
                    sendLiveChatMessage(liveChatSessionId: $id, text: $text)
                }
                """;

        var variables = new { id = liveChatSessionId, text };
        await ExecuteGraphQl<object>(query, variables);
    }

    public async Task CloseLiveChatAsync(Guid liveChatSessionId)
    {
        var query = """
                mutation CloseLiveChat($id: UUID!) {
                    closeLiveChat(liveChatSessionId: $id)
                }
                """;

        var variables = new { id = liveChatSessionId };
        await ExecuteGraphQl<object>(query, variables);
    }

    public async Task<LiveChatSessionDto> StartProactiveChatAsync(string externalUserId, Guid channelId, Guid? channelClientId, string channel)
    {
        var query = """
                mutation StartProactiveChat($externalUserId: String!, $channelId: UUID!, $channelClientId: UUID, $channel: DefaultChannel!) {
                    startProactiveChat(externalUserId: $externalUserId, channelId: $channelId, channelClientId: $channelClientId, channel: $channel) {
                        id
                        externalUserId
                        channel
                        channelId
                        clientChannelId
                        status
                        operatorId
                        createdAt
                    }
                }
                """;

        var variables = new { externalUserId, channelId, channelClientId, channel };
        var result = await ExecuteGraphQl<StartProactiveChatResponse>(query, variables);
        return result.StartProactiveChat ?? throw new InvalidOperationException("Failed to start proactive chat");
    }

    private async Task<T> ExecuteGraphQl<T>(string query, object? variables = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/notification/graphql");
        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request);
        var jsonString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Http Error {response.StatusCode}: {jsonString}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);
        if (gqlResponse == null)
            throw new InvalidOperationException("Сервер вернул пустой ответ.");

        var firstError = gqlResponse.Errors?.Select(e => e.Message).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        if (!string.IsNullOrWhiteSpace(firstError))
            throw new InvalidOperationException(firstError);

        if (gqlResponse.Data == null)
            throw new InvalidOperationException("Не удалось обработать ответ сервера.");

        return gqlResponse.Data;
    }

    private class LiveChatSessionsResponse
    {
        public LiveChatSessionConnection LiveChatSessions { get; set; } = new();
    }

    private class LiveChatSessionConnection
    {
        public List<LiveChatSessionDto> Nodes { get; set; } = [];
    }

    private class LiveChatSessionSingleResponse
    {
        public List<LiveChatSessionDto>? LiveChatSession { get; set; }
    }

    private class StartProactiveChatResponse
    {
        public LiveChatSessionDto? StartProactiveChat { get; set; }
    }
}
