using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Infrastructure.Options;
using TelegramService.Interfaces;

namespace TelegramService.Services;

public sealed class CachingFileSignedUrlProvider(
    FileSignedUrlProvider inner,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<CachingFileSignedUrlProvider> logger) : IFileSignedUrlProvider
{
    private const string KeyPrefix = "file:signed:";

    private int SignedUrlTtlSeconds => cacheOptions.Value.DefaultTtlSeconds is > 0 and <= 600 ? cacheOptions.Value.DefaultTtlSeconds : 120;

    private static string CacheKey(Guid fileId) => $"{KeyPrefix}{fileId}";

    public async Task<ResolvedMedia?> GetSignedUrlAsync(Guid fileId, CancellationToken ct = default)
    {
        var key = CacheKey(fileId);
        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached != null)
            {
                if (cached.Length == 0)
                    return null;
                var dto = JsonConvert.DeserializeObject<ResolvedMediaDto>(cached);
                return dto != null ? new ResolvedMedia(dto.Url, dto.Extension ?? "") : null;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {Key}, calling gRPC", key);
        }

        var result = await inner.GetSignedUrlAsync(fileId, ct);
        try
        {
            var value = result != null ? JsonConvert.SerializeObject(new ResolvedMediaDto(result.Url, result.Extension)) : "";
            var ttl = TimeSpan.FromSeconds(SignedUrlTtlSeconds);
            await cache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {Key}", key);
        }
        return result;
    }

    private sealed record ResolvedMediaDto(string Url, string Extension);
}
