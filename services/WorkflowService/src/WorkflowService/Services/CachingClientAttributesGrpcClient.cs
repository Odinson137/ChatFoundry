using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Options;
using Workflow.Grpc.Client;
using WorkflowService.Interfaces;
using WorkflowService.Models.Dto;

namespace WorkflowService.Services;

public sealed class CachingClientAttributesGrpcClient(
    IClientAttributesGrpcClient inner,
    IDistributedCache cache,
    IOptions<FoundryRedisCacheOptions> cacheOptions,
    ILogger<CachingClientAttributesGrpcClient> logger) : IClientAttributesGrpcClient
{
    private const string KeyPrefix = "client:attrs:";
    private static readonly Newtonsoft.Json.JsonSerializerSettings JsonSettings = new() { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };

    private static string CacheKey(string externalUserId, string channel) => $"{KeyPrefix}{externalUserId}:{channel}";

    public async Task<GetClientAttributesResponse> GetClientAttributesAsync(GetClientAttributesRequest request, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(request.ExternalUserId, request.Channel);
        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);
            if (cached != null)
            {
                var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientAttributesCacheDto>(cached);
                if (dto != null)
                    return ToResponse(dto);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {Key}, calling gRPC", key);
        }

        var response = await inner.GetClientAttributesAsync(request, cancellationToken);
        try
        {
            var dto = ToDto(response);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto, JsonSettings);
            var ttlSeconds = cacheOptions.Value.DefaultTtlSeconds;
            await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds) }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {Key}", key);
        }
        return response;
    }

    public async Task<SetClientAttributesResponse> SetClientAttributesAsync(SetClientAttributesRequest request, CancellationToken cancellationToken = default)
    {
        var response = await inner.SetClientAttributesAsync(request, cancellationToken);
        var key = CacheKey(request.ExternalUserId, request.Channel);
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis remove failed for key {Key}", key);
        }
        return response;
    }

    private static ClientAttributesCacheDto ToDto(GetClientAttributesResponse response)
    {
        var attrs = response.BaseAttributes;
        return new ClientAttributesCacheDto(
            attrs?.Name,
            attrs?.Username,
            attrs?.Phone,
            attrs?.Email,
            new Dictionary<string, string>(response.CustomAttributes));
    }

    private static GetClientAttributesResponse ToResponse(ClientAttributesCacheDto dto)
    {
        var response = new GetClientAttributesResponse();
        var hasAnyBase = dto.Name != null || dto.Username != null || dto.Phone != null || dto.Email != null;
        if (hasAnyBase)
        {
            response.BaseAttributes = new BaseAttributes();
            if (dto.Name != null) response.BaseAttributes.Name = dto.Name;
            if (dto.Username != null) response.BaseAttributes.Username = dto.Username;
            if (dto.Phone != null) response.BaseAttributes.Phone = dto.Phone;
            if (dto.Email != null) response.BaseAttributes.Email = dto.Email;
        }
        if (dto.CustomAttributes != null)
        {
            foreach (var (k, v) in dto.CustomAttributes)
                response.CustomAttributes[k] = v;
        }
        return response;
    }
}
