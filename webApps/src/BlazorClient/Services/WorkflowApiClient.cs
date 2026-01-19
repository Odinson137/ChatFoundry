using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class WorkflowApiClient(HttpClient http) : IWorkflowApiClient
{
    public async Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id)
    {
        var query = new
        {
            query = """
                    query GetWorkflow($id: UUID!) {
                        workflows(where: { id: { eq: $id } }) {
                            id
                            botId
                            nodesDefinition
                            edgesDefinition
                            layoutDefinition
                            version
                            isActiveBotWorkflow
                        }
                    }
                    """,
            variables = new { id }
        };

        var result = await ExecuteGraphQl<GqlWorkflowContent>(query.query, query.variables);
        return result.Workflows.FirstOrDefault();
    }

    public async Task<BotDto?> GetBotWithWorkflowsAsync(Guid botId)
    {
        var query = new
        {
            query = """
                    query GetBot($id: UUID!) {
                        bots(where: { id: { eq: $id } }) {
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
                    """,
            variables = new { id = botId }
        };

        var result = await ExecuteGraphQl<BotDataResponse>(query.query, query.variables);
        return result.Bots.FirstOrDefault();
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
            throw new Exception($"Http Error: {response.StatusCode}");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);

        return gqlResponse!.Data!;
    }
    
    public async Task<bool> AddBotWorkflowAsync(Guid botId, int version)
    {
        var query = new
        {
            query = """
                    mutation Add($input: AddBotWorkflowInput!) {
                        addBotWorkflow(input: $input) {
                            botWorkflow { id }
                        }
                    }
                    """,
            variables = new { 
                input = new { 
                    botId, 
                    version, 
                    nodesDefinition = "[]", 
                    edgesDefinition = "[]", 
                    layoutDefinition = "[]", 
                    isActiveBotWorkflow = false 
                } 
            }
        };
        try { await ExecuteGraphQl<object>(query.query, query.variables); return true; } 
        catch { return false; }
    }
    
    public async Task<bool> UpdateWorkflowDefinitionsAsync(Guid workflowId, string nodes, string edges, string layout)
    {
        var query = new
        {
            query = """
                    mutation UpdateWorkflowDefs($input: UpdateBotWorkflowInput!) {
                        updateBotWorkflow(input: $input) {
                            botWorkflow { id }
                        }
                    }
                    """,
            variables = new
            {
                input = new
                {
                    workflowId,
                    nodesDefinition = nodes,
                    edgesDefinition = edges,
                    layoutDefinition = layout
                }
            }
        };

        try
        {
            await ExecuteGraphQl<object>(query.query, query.variables);
            return true;
        }
        catch (Exception ex)
        {
            // Логирование ошибки может быть полезно для отладки
            Console.WriteLine($"Failed to update workflow definitions: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateBotWorkflowAsync(Guid workflowId, bool isActive)
    {
        var query = new
        {
            query = """
                    mutation Update($input: UpdateBotWorkflowInput!) {
                        updateBotWorkflow(input: $input) {
                            botWorkflow { id }
                        }
                    }
                    """,
            variables = new { 
                input = new { 
                    workflowId, 
                    isActiveBotWorkflow = isActive 
                } 
            }
        };
        try { await ExecuteGraphQl<object>(query.query, query.variables); return true; } 
        catch { return false; }
    }

    public async Task<bool> DeleteBotWorkflowAsync(Guid workflowId)
    {
        var query = new
        {
            query = """
                    mutation Delete($input: DeleteBotWorkflowInput!) {
                        deleteBotWorkflow(input: $input) {
                            botWorkflow { id }
                        }
                    }
                    """,
            variables = new { input = new { workflowId } }
        };
        try { await ExecuteGraphQl<object>(query.query, query.variables); return true; } 
        catch { return false; }
    }

    private class BotDataResponse { public List<BotDto> Bots { get; set; } = []; }
    private class GqlWorkflowContent { public List<WorkflowResponse> Workflows { get; set; } = []; }
}
