using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Shared.Infrastructure.Caching;

public static class DistributedCacheExtensions
{
    public static async Task<T?> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached != null)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)cached;
                }
                return JsonConvert.DeserializeObject<T>(cached);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Redis get failed for key {Key}, calling fallback factory", key);
        }

        var value = await factory();
        if (value == null) return default;

        try
        {
            var options = new DistributedCacheEntryOptions();
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }

            string dataToCache;
            if (typeof(T) == typeof(string))
            {
                dataToCache = (string)(object)value;
            }
            else
            {
                dataToCache = JsonConvert.SerializeObject(value);
            }

            await cache.SetStringAsync(key, dataToCache, options, ct);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Redis set failed for key {Key}", key);
        }

        return value;
    }
}
