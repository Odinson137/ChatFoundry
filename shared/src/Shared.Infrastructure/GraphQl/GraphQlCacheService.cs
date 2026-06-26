using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Shared.Infrastructure.Options;

namespace Shared.Infrastructure.GraphQl;

public interface IGraphQlCacheService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, IEnumerable<string> tags, TimeSpan ttl, CancellationToken ct = default);
    Task EvictByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default);
}

public class GraphQlCacheService : IGraphQlCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;
    private readonly string _keyPrefix;

    public GraphQlCacheService(IConnectionMultiplexer redis, IOptions<FoundryRedisCacheOptions> redisOptions)
    {
        _redis = redis;
        _keyPrefix = redisOptions.Value.KeyPrefix ?? string.Empty;
        _prefix = "cf:graphql:";
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = _keyPrefix + _prefix + "result:" + key;
        return await db.StringGetAsync(fullKey);
    }

    public async Task SetAsync(string key, string value, IEnumerable<string> tags, TimeSpan ttl, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var resultKey = _prefix + "result:" + key;
        var fullResultKey = _keyPrefix + resultKey;

        var batch = db.CreateBatch();

        _ = batch.StringSetAsync(fullResultKey, value, ttl);

        foreach (var tag in tags)
        {
            var tagKey = _keyPrefix + _prefix + "tag:" + tag.ToLower().Trim();
            _ = batch.SetAddAsync(tagKey, fullResultKey);
            _ = batch.KeyExpireAsync(tagKey, ttl.Add(TimeSpan.FromHours(1)));
        }

        batch.Execute();
        await Task.CompletedTask;
    }

    public async Task EvictByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var tagKeys = tags.Select(t => (RedisKey)(_keyPrefix + _prefix + "tag:" + t.ToLower().Trim())).ToArray();

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
