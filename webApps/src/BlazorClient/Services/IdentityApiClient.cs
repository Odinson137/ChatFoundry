using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class IdentityApiClient(HttpClient http) : IIdentityApiClient
{
    public async Task<MeDto?> GetMeAsync(CancellationToken ct = default)
    {
        var query = """
            query GetMe {
                me {
                    id
                    email
                    userName
                    createdAt
                }
            }
            """;
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/identity/graphql");
        request.Content = JsonContent.Create(new { query });

        var response = await http.SendAsync(request, ct);
        var jsonString = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"HTTP {response.StatusCode}: {jsonString}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<MeResponse>>(jsonString, options);
        return gqlResponse?.Data?.Me;
    }

    private class MeResponse
    {
        public MeDto? Me { get; set; }
    }
}
