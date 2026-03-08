using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Options;

namespace ClientService.Services;

public sealed class CachingBotCompanyResolver(
    BotCompanyResolver inner,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<CachingBotCompanyResolver> logger) : Interfaces.IBotCompanyResolver
{
    private const string KeyPrefix = "bot:company:";

    private static string CacheKey(Guid channelId) => $"{KeyPrefix}{channelId}";

    public async Task<Guid?> GetCompanyIdByChannelIdAsync(Guid channelId, CancellationToken ct = default)
    {
        var key = CacheKey(channelId);
        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached != null)
            {
                if (cached.Length == 0)
                    return null;
                return Guid.TryParse(cached, out var id) ? id : null;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {Key}, calling gRPC", key);
        }

        var result = await inner.GetCompanyIdByChannelIdAsync(channelId, ct);
        try
        {
            var value = result.HasValue ? result.Value.ToString() : "";
            var ttl = TimeSpan.FromSeconds(cacheOptions.Value.DefaultTtlSeconds);
            await cache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {Key}", key);
        }
        return result;
    }
}
