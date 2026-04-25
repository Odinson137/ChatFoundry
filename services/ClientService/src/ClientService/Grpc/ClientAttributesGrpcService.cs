using ClientService.Data;
using ClientService.Entities;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Workflow.Grpc.Client;

namespace ClientService.Grpc;

public sealed class ClientAttributesGrpcService(
    ClientDbContext db)
    : ClientAttributesService.ClientAttributesServiceBase
{
    public override async Task<GetClientAttributesResponse> GetClientAttributes(
        GetClientAttributesRequest request,
        ServerCallContext context)
    {
        if (!Enum.TryParse<DefaultChannel>(request.Channel, true, out var channel))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown channel: {request.Channel}"));

        var query = db.ClientChannels
            .Include(c => c.Attributes)
            .Where(c => c.Channel == channel && c.ExternalUserId == request.ExternalUserId);

        if (!string.IsNullOrEmpty(request.ChannelId) && Guid.TryParse(request.ChannelId, out var channelId))
            query = query.Where(c => c.ChannelId == channelId);

        var clientChannel = await query.FirstOrDefaultAsync(context.CancellationToken);

        if (clientChannel == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Client channel not found"));

        var response = new GetClientAttributesResponse
        {
            ClientChannelId = clientChannel.Id.ToString(),
            BaseAttributes = new BaseAttributes
            {
                Name = clientChannel.Name,
                Username = clientChannel.Username,
                Phone = clientChannel.Phone,
                Email = clientChannel.Email
            }
        };

        foreach (var attr in clientChannel.Attributes)
        {
            response.CustomAttributes.Add(attr.Key, attr.Value);
        }

        return response;
    }

    public override async Task<SetClientAttributesResponse> SetClientAttributes(
        SetClientAttributesRequest request,
        ServerCallContext context)
    {
        if (!Enum.TryParse<DefaultChannel>(request.Channel, true, out var channel))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown channel: {request.Channel}"));

        var query = db.ClientChannels
            .Include(c => c.Attributes)
            .Where(c => c.Channel == channel && c.ExternalUserId == request.ExternalUserId);

        if (!string.IsNullOrEmpty(request.ChannelId) && Guid.TryParse(request.ChannelId, out var channelId))
            query = query.Where(c => c.ChannelId == channelId);

        var clientChannel = await query.FirstOrDefaultAsync(context.CancellationToken);

        if (clientChannel == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Client channel not found"));

        var attrs = request.BaseAttributes;
        if (attrs != null)
        {
            if (attrs.Name != null) clientChannel.Name = attrs.Name;
            if (attrs.Username != null) clientChannel.Username = attrs.Username;
            if (attrs.Phone != null) clientChannel.Phone = attrs.Phone;
            if (attrs.Email != null) clientChannel.Email = attrs.Email;
        }

        foreach (var (key, value) in request.CustomAttributes)
        {
            var existing = clientChannel.Attributes.FirstOrDefault(a => a.Key == key);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                clientChannel.Attributes.Add(new ClientAttribute
                {
                    Key = key,
                    Value = value,
                    ClientChannelId = clientChannel.Id
                });
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);

        return new SetClientAttributesResponse { Success = true };
    }

    public override async Task<GetClientsByFilterResponse> GetClientsByFilter(
        GetClientsByFilterRequest request,
        ServerCallContext context)
    {
        var companyId = string.IsNullOrEmpty(request.CompanyId) ? (Guid?)null : Guid.Parse(request.CompanyId);
        var query = db.ClientChannels
            .Include(c => c.Client)
            .Include(c => c.Attributes)
            .Where(c => c.Client != null && (companyId == null || c.Client.CompanyId == companyId));

        if (request.ClientIds.Count > 0)
            query = query.Where(c => request.ClientIds.Contains(c.ExternalUserId));

        if (request.Channels.Count > 0)
            query = query.Where(c => request.Channels.Contains((int)c.Channel));

        if (request.ChannelIds.Count > 0)
        {
            var channelGuids = request.ChannelIds.Select(Guid.Parse).ToList();
            query = query.Where(c => c.ChannelId.HasValue && channelGuids.Contains(c.ChannelId.Value));
        }

        foreach (var condition in request.AttributeConditions)
        {
            query = ApplyAttributeCondition(query, condition);
        }

        var clients = await query
            .Select(c => new FilteredClient
            {
                ExternalUserId = c.ExternalUserId,
                Channel = (int)c.Channel,
                ChannelId = c.ChannelId.ToString(),
            })
            .ToListAsync(context.CancellationToken);

        var response = new GetClientsByFilterResponse();
        response.Clients.AddRange(clients);
        return response;
    }

    private static readonly HashSet<string> BaseAttributeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "username", "phone", "email"
    };

    private static IQueryable<ClientChannel> ApplyAttributeCondition(
        IQueryable<ClientChannel> query,
        ClientAttributeFilterCondition condition)
    {
        var op = condition.Operator.ToLowerInvariant();
        var value = condition.Value;

        if (BaseAttributeKeys.Contains(condition.AttributeKey))
        {
            return ApplyStringFilter(query, condition.AttributeKey, op, value);
        }

        // Custom attribute: filter through the Attributes collection
        query = query.Where(c => c.Attributes.Any(a =>
            a.Key == condition.AttributeKey));

        return op switch
        {
            "equals" => query.Where(c => c.Attributes.Any(a =>
                a.Key == condition.AttributeKey && a.Value == value)),
            "notequals" => query.Where(c => c.Attributes.Any(a =>
                a.Key == condition.AttributeKey && a.Value != value)),
            "contains" => query.Where(c => c.Attributes.Any(a =>
                a.Key == condition.AttributeKey && a.Value.Contains(value))),
            "startswith" => query.Where(c => c.Attributes.Any(a =>
                a.Key == condition.AttributeKey && a.Value.StartsWith(value))),
            "endswith" => query.Where(c => c.Attributes.Any(a =>
                a.Key == condition.AttributeKey && a.Value.EndsWith(value))),
            _ => query
        };
    }

    private static IQueryable<ClientChannel> ApplyStringFilter(
        IQueryable<ClientChannel> query,
        string key,
        string op,
        string value)
    {
        return key.ToLowerInvariant() switch
        {
            "name" => ApplyBaseAttributeFilter(query, op, value,
                c => c.Name != null && EF.Functions.Like(c.Name, GetLikePattern(op, value))),
            "username" => ApplyBaseAttributeFilter(query, op, value,
                c => c.Username != null && EF.Functions.Like(c.Username, GetLikePattern(op, value))),
            "phone" => ApplyBaseAttributeFilter(query, op, value,
                c => c.Phone != null && EF.Functions.Like(c.Phone, GetLikePattern(op, value))),
            "email" => ApplyBaseAttributeFilter(query, op, value,
                c => c.Email != null && EF.Functions.Like(c.Email, GetLikePattern(op, value))),
            _ => query
        };
    }

    private static IQueryable<ClientChannel> ApplyBaseAttributeFilter(
        IQueryable<ClientChannel> query,
        string op,
        string value,
        System.Linq.Expressions.Expression<Func<ClientChannel, bool>> likePredicate)
    {
        return op switch
        {
            "equals" or "contains" or "startswith" or "endswith" => query.Where(likePredicate),
            _ => query
        };
    }

    private static string GetLikePattern(string op, string value) => op switch
    {
        "equals" => value,
        "contains" => $"%{value}%",
        "startswith" => $"{value}%",
        "endswith" => $"%{value}",
        _ => value
    };
}
