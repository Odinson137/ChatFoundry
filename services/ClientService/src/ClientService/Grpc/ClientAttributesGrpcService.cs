using System.Globalization;
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
            response.CustomAttributes.Add(attr.Key, attr.Value);

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
                existing.Value = value;
            else
                clientChannel.Attributes.Add(new ClientAttribute
                {
                    Key = key,
                    Value = value,
                    ClientChannelId = clientChannel.Id
                });
        }

        await db.SaveChangesAsync(context.CancellationToken);
        return new SetClientAttributesResponse { Success = true };
    }

    public override async Task<GetClientsByFilterResponse> GetClientsByFilter(
        GetClientsByFilterRequest request,
        ServerCallContext context)
    {
        var ct = context.CancellationToken;
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

        var isOr = request.ConditionsLogic.Equals("or", StringComparison.OrdinalIgnoreCase);

        if (isOr && request.AttributeConditions.Count > 0)
        {
            // OR: build all subqueries separately, then combine with Union
            IQueryable<ClientChannel>? combined = null;
            foreach (var condition in request.AttributeConditions)
            {
                var subQuery = await ApplyConditionToQuery(query, condition, ct);
                combined = combined == null ? subQuery : combined.Union(subQuery);
            }
            if (combined == null) combined = query;
            query = combined;
        }
        else
        {
            // AND: apply each condition sequentially
            foreach (var condition in request.AttributeConditions)
            {
                query = await ApplyConditionToQuery(query, condition, ct);
            }
        }

        var clients = await query
            .Select(c => new FilteredClient
            {
                ExternalUserId = c.ExternalUserId,
                Channel = (int)c.Channel,
                ChannelId = c.ChannelId.ToString(),
            })
            .ToListAsync(ct);

        var response = new GetClientsByFilterResponse();
        response.Clients.AddRange(clients);
        return response;
    }

    private async Task<IQueryable<ClientChannel>> ApplyConditionToQuery(
        IQueryable<ClientChannel> query,
        ClientAttributeFilterCondition condition,
        CancellationToken ct)
    {
        var op = condition.Operator.ToLowerInvariant();
        if (op is "regex" or "greaterthan" or "lessthan" or "greaterorequal" or "lessorequal")
        {
            var ids = await GetRawMatchIds(condition, ct);
            return ids.Count == 0
                ? query.Where(_ => false)
                : query.Where(c => ids.Contains(c.Id));
        }

        return ApplyLinqCondition(query, condition);
    }

    private async Task<List<Guid>> GetRawMatchIds(
        ClientAttributeFilterCondition condition,
        CancellationToken ct)
    {
        var key = ResolveAttributeKey(condition.AttributeKey);
        var op = condition.Operator.ToLowerInvariant();
        var value = condition.Value;
        var ignoreCase = condition.HasIgnoreCase ? condition.IgnoreCase : DefaultIgnoreCase(op);

        var sql = op switch
        {
            "regex" => ignoreCase
                ? "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND \"Value\" ~* {1}"
                : "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND \"Value\" ~ {1}",
            "greaterthan" when double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                => "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND CAST(\"Value\" AS double precision) > CAST({1} AS double precision)",
            "lessthan" when double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                => "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND CAST(\"Value\" AS double precision) < CAST({1} AS double precision)",
            "greaterorequal" when double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                => "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND CAST(\"Value\" AS double precision) >= CAST({1} AS double precision)",
            "lessorequal" when double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                => "SELECT \"ClientChannelId\" FROM \"ClientAttributes\" WHERE \"Key\" = {0} AND CAST(\"Value\" AS double precision) <= CAST({1} AS double precision)",
            _ => null
        };

        if (sql == null) return [];
        return await db.Database.SqlQueryRaw<Guid>(sql, key, value).ToListAsync(ct);
    }

    // --- Attribute key resolution ---

    private static string ResolveAttributeKey(string key)
    {
        key = key.Trim();
        if (key.StartsWith("{{", StringComparison.Ordinal) && key.EndsWith("}}", StringComparison.Ordinal))
            key = key[2..^2].Trim();
        if (key.StartsWith("$", StringComparison.Ordinal))
            key = key[1..];
        if (key.StartsWith("global.", StringComparison.OrdinalIgnoreCase))
            key = key["global.".Length..];
        return key.Trim();
    }

    // --- Defaults ---

    private static readonly HashSet<string> BaseAttributeKeys = new(StringComparer.OrdinalIgnoreCase)
        { "name", "username", "phone", "email" };

    private static bool DefaultIgnoreCase(string op) => op switch
    {
        "contains" or "startswith" or "endswith" or "inlist" => true,
        _ => false
    };

    // --- LINQ condition applicator (string ops, isempty, inlist) ---

    private static IQueryable<ClientChannel> ApplyLinqCondition(
        IQueryable<ClientChannel> query,
        ClientAttributeFilterCondition condition)
    {
        var op = condition.Operator.ToLowerInvariant();
        var key = ResolveAttributeKey(condition.AttributeKey);
        var value = condition.Value;
        var ignoreCase = condition.HasIgnoreCase ? condition.IgnoreCase : DefaultIgnoreCase(op);

        return BaseAttributeKeys.Contains(key)
            ? ApplyBaseFilter(query, key, op, value, ignoreCase)
            : ApplyCustomFilter(query, key, op, value, ignoreCase);
    }

    // --- Base attributes (name, username, phone, email) — explicit per column for EF Core ---

    private static IQueryable<ClientChannel> ApplyBaseFilter(
        IQueryable<ClientChannel> query, string key, string op, string value, bool ic) =>
        key.ToLowerInvariant() switch
        {
            "name" => ApplyBaseName(query, op, value, ic),
            "username" => ApplyBaseUsername(query, op, value, ic),
            "phone" => ApplyBasePhone(query, op, value, ic),
            "email" => ApplyBaseEmail(query, op, value, ic),
            _ => query
        };

    private static IQueryable<ClientChannel> ApplyBaseName(IQueryable<ClientChannel> q, string op, string v, bool ic)
    {
        var items = op == "inlist"
            ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;
        return op switch
        {
            "equals" when ic => q.Where(c => EF.Functions.ILike(c.Name!, v)),
            "equals" => q.Where(c => c.Name == v),
            "notequals" when ic => q.Where(c => !EF.Functions.ILike(c.Name!, v)),
            "notequals" => q.Where(c => c.Name != v),
            "contains" when ic => q.Where(c => EF.Functions.ILike(c.Name!, $"%{v}%")),
            "contains" => q.Where(c => EF.Functions.Like(c.Name!, $"%{v}%")),
            "startswith" when ic => q.Where(c => EF.Functions.ILike(c.Name!, $"{v}%")),
            "startswith" => q.Where(c => EF.Functions.Like(c.Name!, $"{v}%")),
            "endswith" when ic => q.Where(c => EF.Functions.ILike(c.Name!, $"%{v}")),
            "endswith" => q.Where(c => EF.Functions.Like(c.Name!, $"%{v}")),
            "isempty" => q.Where(c => string.IsNullOrEmpty(c.Name)),
            "isnotempty" => q.Where(c => !string.IsNullOrEmpty(c.Name)),
            "inlist" when ic && items != null => q.Where(c => items.Any(item => EF.Functions.ILike(c.Name!, item))),
            "inlist" when items != null => q.Where(c => items.Contains(c.Name!)),
            _ => q
        };
    }

    private static IQueryable<ClientChannel> ApplyBaseUsername(IQueryable<ClientChannel> q, string op, string v, bool ic)
    {
        var items = op == "inlist"
            ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;
        return op switch
        {
            "equals" when ic => q.Where(c => EF.Functions.ILike(c.Username!, v)),
            "equals" => q.Where(c => c.Username == v),
            "notequals" when ic => q.Where(c => !EF.Functions.ILike(c.Username!, v)),
            "notequals" => q.Where(c => c.Username != v),
            "contains" when ic => q.Where(c => EF.Functions.ILike(c.Username!, $"%{v}%")),
            "contains" => q.Where(c => EF.Functions.Like(c.Username!, $"%{v}%")),
            "startswith" when ic => q.Where(c => EF.Functions.ILike(c.Username!, $"{v}%")),
            "startswith" => q.Where(c => EF.Functions.Like(c.Username!, $"{v}%")),
            "endswith" when ic => q.Where(c => EF.Functions.ILike(c.Username!, $"%{v}")),
            "endswith" => q.Where(c => EF.Functions.Like(c.Username!, $"%{v}")),
            "isempty" => q.Where(c => string.IsNullOrEmpty(c.Username)),
            "isnotempty" => q.Where(c => !string.IsNullOrEmpty(c.Username)),
            "inlist" when ic && items != null => q.Where(c => items.Any(item => EF.Functions.ILike(c.Username!, item))),
            "inlist" when items != null => q.Where(c => items.Contains(c.Username!)),
            _ => q
        };
    }

    private static IQueryable<ClientChannel> ApplyBasePhone(IQueryable<ClientChannel> q, string op, string v, bool ic)
    {
        var items = op == "inlist"
            ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;
        return op switch
        {
            "equals" when ic => q.Where(c => EF.Functions.ILike(c.Phone!, v)),
            "equals" => q.Where(c => c.Phone == v),
            "notequals" when ic => q.Where(c => !EF.Functions.ILike(c.Phone!, v)),
            "notequals" => q.Where(c => c.Phone != v),
            "contains" when ic => q.Where(c => EF.Functions.ILike(c.Phone!, $"%{v}%")),
            "contains" => q.Where(c => EF.Functions.Like(c.Phone!, $"%{v}%")),
            "startswith" when ic => q.Where(c => EF.Functions.ILike(c.Phone!, $"{v}%")),
            "startswith" => q.Where(c => EF.Functions.Like(c.Phone!, $"{v}%")),
            "endswith" when ic => q.Where(c => EF.Functions.ILike(c.Phone!, $"%{v}")),
            "endswith" => q.Where(c => EF.Functions.Like(c.Phone!, $"%{v}")),
            "isempty" => q.Where(c => string.IsNullOrEmpty(c.Phone)),
            "isnotempty" => q.Where(c => !string.IsNullOrEmpty(c.Phone)),
            "inlist" when ic && items != null => q.Where(c => items.Any(item => EF.Functions.ILike(c.Phone!, item))),
            "inlist" when items != null => q.Where(c => items.Contains(c.Phone!)),
            _ => q
        };
    }

    private static IQueryable<ClientChannel> ApplyBaseEmail(IQueryable<ClientChannel> q, string op, string v, bool ic)
    {
        var items = op == "inlist"
            ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;
        return op switch
        {
            "equals" when ic => q.Where(c => EF.Functions.ILike(c.Email!, v)),
            "equals" => q.Where(c => c.Email == v),
            "notequals" when ic => q.Where(c => !EF.Functions.ILike(c.Email!, v)),
            "notequals" => q.Where(c => c.Email != v),
            "contains" when ic => q.Where(c => EF.Functions.ILike(c.Email!, $"%{v}%")),
            "contains" => q.Where(c => EF.Functions.Like(c.Email!, $"%{v}%")),
            "startswith" when ic => q.Where(c => EF.Functions.ILike(c.Email!, $"{v}%")),
            "startswith" => q.Where(c => EF.Functions.Like(c.Email!, $"{v}%")),
            "endswith" when ic => q.Where(c => EF.Functions.ILike(c.Email!, $"%{v}")),
            "endswith" => q.Where(c => EF.Functions.Like(c.Email!, $"%{v}")),
            "isempty" => q.Where(c => string.IsNullOrEmpty(c.Email)),
            "isnotempty" => q.Where(c => !string.IsNullOrEmpty(c.Email)),
            "inlist" when ic && items != null => q.Where(c => items.Any(item => EF.Functions.ILike(c.Email!, item))),
            "inlist" when items != null => q.Where(c => items.Contains(c.Email!)),
            _ => q
        };
    }

    // --- Custom attributes ---

    private static IQueryable<ClientChannel> ApplyCustomFilter(
        IQueryable<ClientChannel> query, string key, string op, string value, bool ic) => op switch
    {
        "equals" when ic => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.ILike(a.Value, value))),
        "equals" => query.Where(c => c.Attributes.Any(a => a.Key == key && a.Value == value)),
        "notequals" when ic => query.Where(c => c.Attributes.Any(a => a.Key == key && !EF.Functions.ILike(a.Value, value))),
        "notequals" => query.Where(c => c.Attributes.Any(a => a.Key == key && a.Value != value)),
        "contains" when ic => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.ILike(a.Value, $"%{value}%"))),
        "contains" => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.Like(a.Value, $"%{value}%"))),
        "startswith" when ic => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.ILike(a.Value, $"{value}%"))),
        "startswith" => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.Like(a.Value, $"{value}%"))),
        "endswith" when ic => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.ILike(a.Value, $"%{value}"))),
        "endswith" => query.Where(c => c.Attributes.Any(a => a.Key == key && EF.Functions.Like(a.Value, $"%{value}"))),
        "isempty" => query.Where(c => c.Attributes.Any(a => a.Key == key && string.IsNullOrEmpty(a.Value))),
        "isnotempty" => query.Where(c => c.Attributes.Any(a => a.Key == key && !string.IsNullOrEmpty(a.Value))),
        "inlist" => CustomInList(query, key, value, ic),
        _ => query
    };

    private static IQueryable<ClientChannel> CustomInList(
        IQueryable<ClientChannel> query, string key, string value, bool ic)
    {
        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return ic
            ? query.Where(c => c.Attributes.Any(a => a.Key == key && items.Any(item => EF.Functions.ILike(a.Value, item))))
            : query.Where(c => c.Attributes.Any(a => a.Key == key && items.Contains(a.Value)));
    }
}
