using ClientService.Interfaces;
using Grpc.Core;
using Shared.Domain.Enums;
using Workflow.Grpc.Client;

namespace ClientService.Grpc;

public sealed class ClientAttributesGrpcService(
    IClientChannelRepository clientChannelRepository)
    : ClientAttributesService.ClientAttributesServiceBase
{
    public override async Task<GetClientAttributesResponse> GetClientAttributes(
        GetClientAttributesRequest request,
        ServerCallContext context)
    {
        if (!Enum.TryParse<DefaultChannel>(request.Channel, true, out var channel))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown channel: {request.Channel}"));

        var clientChannel = await clientChannelRepository.FindAsync(
            channel, request.ExternalUserId, context.CancellationToken);

        if (clientChannel == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Client channel not found"));

        var response = new GetClientAttributesResponse
        {
            BaseAttributes = new BaseAttributes
            {
                Name = clientChannel.Name,
                Username = clientChannel.Username,
                Phone = clientChannel.Phone,
                Email = clientChannel.Email
            }
        };

        return response;
    }

    public override async Task<SetClientAttributesResponse> SetClientAttributes(
        SetClientAttributesRequest request,
        ServerCallContext context)
    {
        if (!Enum.TryParse<DefaultChannel>(request.Channel, true, out var channel))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown channel: {request.Channel}"));

        var clientChannel = await clientChannelRepository.FindAsync(
            channel, request.ExternalUserId, context.CancellationToken);

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

        await clientChannelRepository.SaveAsync(clientChannel, context.CancellationToken);

        return new SetClientAttributesResponse { Success = true };
    }
}
