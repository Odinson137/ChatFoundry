using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public static class HttpClientGraphQlExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> PostGraphQlAsync<T>(
        this HttpClient http,
        string endpoint,
        string query,
        object? variables = null,
        CancellationToken ct = default)
    {
        var queryHash = ComputeSha256(query);

        var isMutation = query.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase);
        if (isMutation)
        {
            return await ExecuteGraphQlPostAsync<T>(http, endpoint, query, queryHash, variables, ct);
        }

        var extensions = new
        {
            persistedQuery = new
            {
                version = 1,
                sha256Hash = queryHash
            }
        };

        var queryParams = new List<string>
        {
            $"extensions={Uri.EscapeDataString(JsonSerializer.Serialize(extensions))}"
        };

        if (variables != null)
        {
            queryParams.Add($"variables={Uri.EscapeDataString(JsonSerializer.Serialize(variables))}");
        }

        var getUri = $"{endpoint}?{string.Join("&", queryParams)}";
        var response = await http.GetAsync(getUri, ct);
        var jsonString = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, JsonOptions);

            if (gqlResponse?.Errors != null && gqlResponse.Errors.Any(e => e.Message == "PersistedQueryNotFound"))
            {
                return await ExecuteGraphQlPostAsync<T>(http, endpoint, query, queryHash, variables, ct);
            }

            if (gqlResponse?.Errors != null && gqlResponse.Errors.Count > 0)
            {
                throw new InvalidOperationException(gqlResponse.Errors[0].Message ?? "GraphQL Error");
            }

            return gqlResponse!.Data!;
        }

        throw new Exception($"Http Error {response.StatusCode}: {jsonString}");
    }

    private static async Task<T> ExecuteGraphQlPostAsync<T>(
        HttpClient http,
        string endpoint,
        string query,
        string hash,
        object? variables,
        CancellationToken ct)
    {
        var payload = new
        {
            query = query,
            variables = variables,
            extensions = new
            {
                persistedQuery = new
                {
                    version = 1,
                    sha256Hash = hash
                }
            }
        };

        var response = await http.PostAsJsonAsync(endpoint, payload, ct);
        var jsonString = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Http Error {response.StatusCode}: {jsonString}");
        }

        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, JsonOptions);

        if (gqlResponse?.Errors != null && gqlResponse.Errors.Count > 0)
        {
            throw new InvalidOperationException(gqlResponse.Errors[0].Message ?? "GraphQL Error");
        }

        return gqlResponse!.Data!;
    }

    private static string ComputeSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
