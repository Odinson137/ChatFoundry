using System.Text.Json.Serialization;

namespace BlazorClient.Models.DTO;

public class GraphQLResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}