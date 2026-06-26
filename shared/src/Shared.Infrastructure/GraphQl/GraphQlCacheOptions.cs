namespace Shared.Infrastructure.GraphQl;

public class GraphQlCacheOptions
{
    public const string SectionName = "GraphQlCache";

    public List<string> CacheableQueries { get; set; } = new();

    public Dictionary<string, List<string>> QueryTags { get; set; } = new();

    public Dictionary<string, int> QueryTtls { get; set; } = new();

    public int DefaultTtlSeconds { get; set; } = 60;
}
