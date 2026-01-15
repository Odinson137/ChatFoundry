using System.Net;
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
    
    private async Task<T> ExecuteGraphQl<T>(string query, object? variables = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/workflow/graphql");
        
        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request);
        var jsonString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Сервер вернул ошибку {response.StatusCode}: {jsonString}");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);

        return gqlResponse!.Data!;
    }
}