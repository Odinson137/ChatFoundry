using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Options;
using TelegramService.Interfaces;

namespace TelegramService.Services;

public sealed class CachingBotTokenProvider(
    GrpcBotTokenProvider inner,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<CachingBotTokenProvider> logger) : IBotTokenProvider
{
    private const string KeyPrefix = "bot:token:";

    private static string CacheKey(Guid channelId) => $"{KeyPrefix}{channelId}";

    public async Task<string> GetByChannelIdAsync(Guid channelId, CancellationToken ct)
    {
        var key = CacheKey(channelId);
        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {Key}, calling gRPC", key);
        }

        var token = await inner.GetByChannelIdAsync(channelId, ct);
        try
        {
            var ttl = TimeSpan.FromSeconds(cacheOptions.Value.DefaultTtlSeconds);
            await cache.SetStringAsync(key, token, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {Key}", key);
        }
        return token;
    }
}
