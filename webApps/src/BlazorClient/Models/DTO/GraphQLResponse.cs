using System.Text.Json.Serialization;

namespace BlazorClient.Models.DTO;

public class GraphQLResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQlErrorDto>? Errors { get; set; }
}

public class GraphQlErrorDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}