namespace Shared.Infrastructure.Options;

public class FoundryRedisCacheOptions
{
    public const string SectionName = "Cache";

    public string? ConnectionString { get; set; }

    public string? Host { get; set; }
    public int Port { get; set; } = 6379;

    public string KeyPrefix { get; set; } = "foundry:";

    public int DefaultTtlSeconds { get; set; } = 300;
}
