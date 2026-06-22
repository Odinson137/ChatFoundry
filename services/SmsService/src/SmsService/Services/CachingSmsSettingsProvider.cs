using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Options;
using SmsService.Interfaces;

namespace SmsService.Services;

public sealed class CachingSmsSettingsProvider(
    SmsSettingsProvider inner,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<CachingSmsSettingsProvider> logger) : ISmsSettingsProvider
{
    private const string KeyPrefix = "bot:token:sms:";

    private static string CacheKey(Guid channelId) => $"{KeyPrefix}{channelId}";

    public Task<string> GetSenderPhoneByChannelIdAsync(Guid channelId, CancellationToken ct)
    {
        var key = CacheKey(channelId);
        var ttl = TimeSpan.FromSeconds(cacheOptions.Value.DefaultTtlSeconds);

        return cache.GetOrSetAsync(
            key,
            () => inner.GetSenderPhoneByChannelIdAsync(channelId, ct),
            ttl,
            logger,
            ct)!;
    }
}
