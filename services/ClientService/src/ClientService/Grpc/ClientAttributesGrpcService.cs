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
}
