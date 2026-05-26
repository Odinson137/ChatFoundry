using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatFoundry.TestInfrastructure.Auth;

namespace ChatFoundry.TestInfrastructure.Extensions;

public static class HttpClientExtensions
{
    public static HttpClient WithAuth(this HttpClient client, Guid userId, Guid? companyId = null, string[]? scopes = null)
    {
        var token = TestJwtGenerator.GenerateToken(userId, companyId, scopes);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<T> PostGraphQlAsync<T>(this HttpClient client, string query, object? variables = null, string endpoint = "/graphql")
    {
        var requestBody = new
        {
            query = query,
            variables = variables
        };

        var response = await client.PostAsJsonAsync(endpoint, requestBody);
        response.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var graphQlResponse = await response.Content.ReadFromJsonAsync<GraphQlResponse<T>>(jsonOptions);

        if (graphQlResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize GraphQL response.");
        }

        if (graphQlResponse.Errors != null && graphQlResponse.Errors.Any())
        {
            var errorsStr = string.Join("; ", graphQlResponse.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"GraphQL errors: {errorsStr}");
        }

        return graphQlResponse.Data;
    }

    private class GraphQlResponse<TData>
    {
        public TData Data { get; set; } = default!;
        public List<GraphQlError>? Errors { get; set; }
    }

    private class GraphQlError
    {
        public string Message { get; set; } = "";
    }
}
