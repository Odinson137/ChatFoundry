using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NotificationService.Interfaces;
using Shared.Infrastructure.Options;
using Workflow.Grpc.Client;

namespace NotificationService.Services;

public sealed class ClientChannelResolverService(
    ClientAttributesService.ClientAttributesServiceClient grpcClient,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<ClientChannelResolverService> logger) : IClientAttributesService
{
    private const string KeyPrefix = "client:attrs:";

    private static string CacheKey(string externalUserId, string channel, Guid? channelId)
        => channelId.HasValue
            ? $"{KeyPrefix}{externalUserId}:{channel}:{channelId}"
            : $"{KeyPrefix}{externalUserId}:{channel}";

    public async Task<ClientChannelInfo?> GetClientChannelInfoAsync(
        string externalUserId,
        string channel,
        Guid? channelId,
        CancellationToken ct)
    {
        var key = CacheKey(externalUserId, channel, channelId);

        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached != null)
            {
                var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientChannelCacheDto>(cached);
                if (dto != null)
                    return new ClientChannelInfo(dto.Id, dto.Name, dto.Username);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {Key}", key);
        }

        var request = new GetClientAttributesRequest
        {
            ExternalUserId = externalUserId,
            Channel = channel
        };

        if (channelId.HasValue)
            request.ChannelId = channelId.Value.ToString();

        Workflow.Grpc.Client.GetClientAttributesResponse response;
        try
        {
            response = await grpcClient.GetClientAttributesAsync(request, cancellationToken: ct);
        }
        catch (Grpc.Core.RpcException)
        {
            return null;
        }

        var info = new ClientChannelInfo(
            Guid.Parse(response.ClientChannelId),
            response.BaseAttributes?.Name,
            response.BaseAttributes?.Username);

        try
        {
            var dto = new ClientChannelCacheDto(info.Id, info.Name, info.Username);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            var ttlSeconds = cacheOptions.Value.DefaultTtlSeconds;
            await cache.SetStringAsync(key, json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds) }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {Key}", key);
        }

        return info;
    }

    private sealed record ClientChannelCacheDto(Guid Id, string? Name, string? Username);
}
