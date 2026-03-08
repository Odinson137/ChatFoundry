namespace Shared.Infrastructure.Options;

public class FoundryRedisCacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Redis connection string (e.g. "redis:6379"). Used when set; otherwise built from Host and Port.</summary>
    public string? ConnectionString { get; set; }

    public string? Host { get; set; }
    public int Port { get; set; } = 6379;

    /// <summary>Key prefix for all cache keys (e.g. "foundry:").</summary>
    public string KeyPrefix { get; set; } = "foundry:";

    /// <summary>Default TTL in seconds for cached entries.</summary>
    public int DefaultTtlSeconds { get; set; } = 300;
}
