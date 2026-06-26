using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace Shared.Infrastructure.GraphQl;

public interface IGraphQlCacheService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, IEnumerable<string> tags, TimeSpan ttl, CancellationToken ct = default);
    Task EvictByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default);
}

public class GraphQlCacheService : IGraphQlCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    public GraphQlCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
        _prefix = "cf:graphql:";
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        return await _cache.GetStringAsync(_prefix + "result:" + key, ct);
    }

    public async Task SetAsync(string key, string value, IEnumerable<string> tags, TimeSpan ttl, CancellationToken ct = default)
    {
        var resultKey = _prefix + "result:" + key;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await _cache.SetStringAsync(resultKey, value, options, ct);

        var db = _redis.GetDatabase();
        var batch = db.CreateBatch();

        foreach (var tag in tags)
        {
            var tagKey = _prefix + "tag:" + tag.ToLower().Trim();
            _ = batch.SetAddAsync(tagKey, resultKey);
            _ = batch.KeyExpireAsync(tagKey, ttl.Add(TimeSpan.FromHours(1)));
        }

        batch.Execute();
    }

    public async Task EvictByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var tagKeys = tags.Select(t => (RedisKey)(_prefix + "tag:" + t.ToLower().Trim())).ToArray();

        foreach (var tagKey in tagKeys)
        {
            var members = await db.SetMembersAsync(tagKey);
            if (members.Length > 0)
            {
                var keysToDelete = members.Select(m => (RedisKey)m.ToString()).ToArray();
                await db.KeyDeleteAsync(keysToDelete);
            }
            await db.KeyDeleteAsync(tagKey);
        }
    }
}
