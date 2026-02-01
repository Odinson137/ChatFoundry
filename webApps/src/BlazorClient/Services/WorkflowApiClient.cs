using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class WorkflowApiClient(HttpClient http) : IWorkflowApiClient
{
    // ─── Bots ────────────────────────────────────────────────────────────────

    public async Task<List<BotDto>> GetBotsAsync()
    {
        var query = """
                query GetBots {
                    bots(order: [{ createdAt: DESC }]) {
                        nodes {
                            id
                            name
                            token
                            createdAt
                            modifiedAt
                            workflows {
                                id
                                version
                                isActiveBotWorkflow
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
                            token
                            createdAt
                            modifiedAt
                            workflows {
                                id
                                version
                                isActiveBotWorkflow
                                createdAt
                            }
                        }
                    }
                }
                """;

        var variables = new { id = botId };
        var result = await ExecuteGraphQl<BotsConnectionResponse>(query, variables);
        return result.Bots.Nodes.FirstOrDefault();
    }

    public async Task<BotDto> AddBotAsync(string name, string token)
    {
        var query = """
                mutation Add($input: AddBotInput!) {
                    addBot(input: $input) {
                        bot { id name }
                    }
                }
                """;

        var variables = new { input = new { name, token } };
        var result = await ExecuteGraphQl<AddBotResponse>(query, variables);
        return result.AddBot.Bot;
    }

    public async Task<BotDto> UpdateBotAsync(Guid botId, string name)
    {
        var query = """
                mutation Update($input: UpdateBotInput!) {
                    updateBot(input: $input) {
                        bot { id name }
                    }
                }
                """;

        var variables = new { input = new { botId, name } };
        var result = await ExecuteGraphQl<UpdateBotResponse>(query, variables);
        return result.UpdateBot.Bot;
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
                        }
                    }
                }
                """;

        var variables = new { id };
        var result = await ExecuteGraphQl<WorkflowsConnectionResponse>(query, variables);
        return result.Workflows.Nodes.FirstOrDefault();
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

    public async Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout)
    {
        var query = """
                mutation UpdateWorkflowDefs($input: UpdateBotWorkflowInput!) {
                    updateBotWorkflow(input: $input) {
                        botWorkflow { id }
                    }
                }
                """;

        var variables = new
        {
            input = new
            {
                workflowId,
                nodesDefinition = nodes,
                edgesDefinition = edges,
                layoutDefinition = layout
            }
        };

        try { await ExecuteGraphQl<object>(query, variables); return true; }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update workflow definitions: {ex.Message}");
            return false;
        }
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
        public BotDto Bot { get; set; } = new();
    }
}