using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class ClientApiClient(HttpClient http) : IClientApiClient
{
    public async Task<ClientsPageResult> GetClientsAsync(
        int first,
        string? after = null,
        string? search = null,
        string? channelFilter = null,
        string sortField = "createdAt",
        string sortDirection = "DESC")
    {
        var whereArg = !string.IsNullOrWhiteSpace(channelFilter)
            ? "where: { clientChannels: { some: { channel: { eq: $channel } } } }"
            : "";

        var orderArg = $"order: [{{ {sortField}: {sortDirection} }}]";

        var afterArg = after != null ? ", after: $after" : "";
        var searchArg = !string.IsNullOrWhiteSpace(search) ? ", search: $search" : "";


        var varDecls = new List<string> { "$first: Int!" };
        if (after != null) varDecls.Add("$after: String!");
        if (!string.IsNullOrWhiteSpace(search)) varDecls.Add("$search: String!");
        if (!string.IsNullOrWhiteSpace(channelFilter)) varDecls.Add("$channel: DefaultChannel!");

        var query = $$"""
                query GetClients({{string.Join(", ", varDecls)}}) {
                    clients(first: $first{{afterArg}}{{searchArg}}, {{orderArg}}{{(whereArg.Length > 0 ? ", " + whereArg : "")}}) {
                        totalCount
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            endCursor
                            startCursor
                        }
                        nodes {
                            id
                            displayName
                            createdAt
                            modifiedAt
                            clientChannels {
                                id
                                channelId
                                channel
                                externalUserId
                                phone
                                email
                                username
                                name
                                lastName
                                createdAt
                                attributes {
                                    key
                                    value
                                }
                            }
                        }
                    }
                }
                """;

        var variables = new Dictionary<string, object?> { ["first"] = first };
        if (after != null) variables["after"] = after;
        if (!string.IsNullOrWhiteSpace(search)) variables["search"] = search;
        if (!string.IsNullOrWhiteSpace(channelFilter)) variables["channel"] = channelFilter.ToUpperInvariant();

        var result = await ExecuteGraphQl<ClientsConnectionResponse>(query, variables);
        var connection = result.Clients;

        return new ClientsPageResult
        {
            Items = connection.Nodes,
            TotalCount = connection.TotalCount,
            HasNextPage = connection.PageInfo.HasNextPage,
            HasPreviousPage = connection.PageInfo.HasPreviousPage,
            EndCursor = connection.PageInfo.EndCursor,
            StartCursor = connection.PageInfo.StartCursor
        };
    }

    public async Task<ClientDto?> GetClientByIdAsync(Guid clientId)
    {
        var query = """
            query GetClientById($id: UUID!) {
                clients(first: 1, where: { id: { eq: $id } }) {
                    nodes {
                        id
                        displayName
                        createdAt
                        modifiedAt
                        clientChannels {
                            id
                            channelId
                            channel
                            externalUserId
                            phone
                            email
                            username
                            name
                            lastName
                            createdAt
                            attributes {
                                key
                                value
                            }
                        }
                    }
                }
            }
            """;

        var variables = new Dictionary<string, object?> { ["id"] = clientId };
        var result = await ExecuteGraphQl<ClientsConnectionResponse>(query, variables);
        return result.Clients.Nodes.FirstOrDefault();
    }

    public async Task<Guid?> GetClientIdByChannelIdAsync(Guid clientChannelId)
    {
        var query = """
            query GetClientByChannelId($channelId: UUID!) {
                clients(first: 1, where: { clientChannels: { some: { id: { eq: $channelId } } } }) {
                    nodes { id }
                }
            }
            """;

        var variables = new Dictionary<string, object?> { ["channelId"] = clientChannelId };
        var result = await ExecuteGraphQl<ClientsConnectionResponse>(query, variables);
        return result.Clients.Nodes.FirstOrDefault()?.Id;
    }

    public async Task<MessagesPageResult> GetMessagesAsync(Guid clientChannelId, int first, string? after = null)
    {
        var varDecls = new List<string> { "$clientChannelId: UUID!", "$first: Int!" };
        var afterArg = "";
        if (after != null)
        {
            varDecls.Add("$after: String!");
            afterArg = ", after: $after";
        }

        var query = $$"""
            query GetMessages({{string.Join(", ", varDecls)}}) {
                messages(
                    first: $first{{afterArg}},
                    where: { clientChannel: { id: { eq: $clientChannelId } } },
                    order: [{ createdAt: DESC }]
                ) {
                    totalCount
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                    nodes {
                        id
                        payload
                        direction
                        messageKind
                        createdAt
                    }
                }
            }
            """;

        var variables = new Dictionary<string, object?> { ["clientChannelId"] = clientChannelId, ["first"] = first };
        if (after != null) variables["after"] = after;

        var result = await ExecuteGraphQl<MessagesConnectionResponse>(query, variables);
        var connection = result.Messages;

        return new MessagesPageResult
        {
            Items = connection.Nodes,
            TotalCount = connection.TotalCount,
            HasNextPage = connection.PageInfo.HasNextPage,
            EndCursor = connection.PageInfo.EndCursor
        };
    }



    public async Task<MessagesPageResult> GetMessagesByChannelAsync(Guid channelId, string externalUserId, string channel, int first)
    {
        var query = """
            query GetMessagesByChannel($channelId: UUID!, $externalUserId: String!, $channel: DefaultChannel!, $first: Int!) {
                messages(
                    first: $first,
                    where: {
                        clientChannel: {
                            channelId: { eq: $channelId },
                            externalUserId: { eq: $externalUserId },
                            channel: { eq: $channel }
                        }
                    },
                    order: [{ createdAt: DESC }]
                ) {
                    totalCount
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                    nodes {
                        id
                        payload
                        direction
                        messageKind
                        createdAt
                    }
                }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["channelId"] = channelId,
            ["externalUserId"] = externalUserId,
            ["channel"] = channel.ToUpperInvariant(),
            ["first"] = first
        };

        var result = await ExecuteGraphQl<MessagesConnectionResponse>(query, variables);
        var connection = result.Messages;

        return new MessagesPageResult
        {
            Items = connection.Nodes,
            TotalCount = connection.TotalCount,
            HasNextPage = connection.PageInfo.HasNextPage,
            EndCursor = connection.PageInfo.EndCursor
        };
    }

    public async Task<List<AttributeDefinitionDto>> GetCompanyAttributeDefinitionsAsync(CancellationToken ct = default)
    {
        var query = """
            query GetCompanyAttributeDefinitions {
                companyAttributeDefinitions {
                    id key displayName description type scope scopeEntityId
                }
            }
            """;
        var result = await ExecuteGraphQl<CompanyAttributeDefinitionsResponse>(query, null, ct);
        return result.CompanyAttributeDefinitions;
    }

    public async Task<AttributeDefinitionDto> CreateCompanyAttributeDefinitionAsync(string key, string? displayName,
        string? description, CancellationToken ct = default)
    {
        var query = """
            mutation CreateCompanyAttributeDefinition($key: String!, $displayName: String, $description: String) {
                createCompanyAttributeDefinition(key: $key, displayName: $displayName, description: $description) {
                    id key displayName description type scope scopeEntityId
                }
            }
            """;
        var variables = new
        {
            key,
            displayName,
            description
        };
        var result = await ExecuteGraphQl<CreateCompanyAttributeDefinitionResponse>(query, variables, ct);
        return result.CreateCompanyAttributeDefinition;
    }

    public async Task<AttributeDefinitionDto?> UpdateAttributeDefinitionAsync(Guid id, string? displayName, string? description, AttributeType? type, CancellationToken ct = default)
    {
        var query = """
            mutation UpdateAttributeDefinition($id: UUID!, $displayName: String, $description: String) {
                updateAttributeDefinition(id: $id, displayName: $displayName, description: $description) {
                    id key displayName description type scope scopeEntityId
                }
            }
            """;
        var variables = new { id, displayName, description, type = type?.ToString() };
        var result = await ExecuteGraphQl<UpdateAttributeDefinitionResponse>(query, variables, ct);
        return result.UpdateAttributeDefinition;
    }

    public async Task<bool> DeleteAttributeDefinitionAsync(Guid id, CancellationToken ct = default)
    {
        var query = """
            mutation DeleteAttributeDefinition($id: UUID!) {
                deleteAttributeDefinition(id: $id)
            }
            """;
        var result = await ExecuteGraphQl<DeleteAttributeDefinitionResponse>(query, new { id }, ct);
        return result.DeleteAttributeDefinition;
    }

    public async Task<ClientChannelDto?> SetClientChannelAttributesAsync(SetClientChannelAttributesRequest request, CancellationToken ct = default)
    {
        var query = """
            mutation SetClientChannelAttributes($input: SetClientChannelAttributesInput!) {
                setClientChannelAttributes(input: $input) {
                    id channelId channel externalUserId
                    phone email username name lastName
                    createdAt
                    attributes { key value }
                }
            }
            """;
        var variables = new
        {
            input = new
            {
                request.ClientChannelId,
                request.Name,
                request.LastName,
                request.Username,
                request.Phone,
                request.Email,
                customAttributes = request.CustomAttributes?.Select(a => new { a.Key, a.Value })
            }
        };
        var result = await ExecuteGraphQl<SetClientChannelAttributesResponse>(query, variables, ct);
        return result.SetClientChannelAttributes;
    }



    private async Task<T> ExecuteGraphQl<T>(string query, object? variables = null, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/client/graphql");
        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request, ct);
        var jsonString = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Http Error {response.StatusCode}: {jsonString}");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);

        return gqlResponse!.Data!;
    }



    private class ClientsConnectionResponse
    {
        public ClientConnection Clients { get; set; } = new();
    }

    private class ClientConnection
    {
        public int TotalCount { get; set; }
        public PageInfoDto PageInfo { get; set; } = new();
        public List<ClientDto> Nodes { get; set; } = [];
    }

    private class PageInfoDto
    {
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? EndCursor { get; set; }
        public string? StartCursor { get; set; }
    }

    private class MessagesConnectionResponse
    {
        public MessageConnection Messages { get; set; } = new();
    }

    private class MessageConnection
    {
        public int TotalCount { get; set; }
        public PageInfoDto PageInfo { get; set; } = new();
        public List<MessageDto> Nodes { get; set; } = [];
    }

    private class CompanyAttributeDefinitionsResponse
    {
        public List<AttributeDefinitionDto> CompanyAttributeDefinitions { get; set; } = [];
    }

    private class CreateAttributeDefinitionResponse
    {
        public AttributeDefinitionDto CreateAttributeDefinition { get; set; } = new();
    }

    private class CreateCompanyAttributeDefinitionResponse
    {
        public AttributeDefinitionDto CreateCompanyAttributeDefinition { get; set; } = new();
    }

    private class UpdateAttributeDefinitionResponse
    {
        public AttributeDefinitionDto? UpdateAttributeDefinition { get; set; }
    }

    private class DeleteAttributeDefinitionResponse
    {
        public bool DeleteAttributeDefinition { get; set; }
    }

    private class SetClientChannelAttributesResponse
    {
        public ClientChannelDto? SetClientChannelAttributes { get; set; }
    }
}
