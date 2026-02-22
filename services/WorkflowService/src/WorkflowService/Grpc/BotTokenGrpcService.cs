using Grpc.Core;
using Workflow.Grpc;
using WorkflowService.Interfaces;

namespace WorkflowService.Grpc;

public sealed class BotTokenGrpcService : BotTokenService.BotTokenServiceBase
{
    private readonly IChannelRepository _channelRepository;

    public BotTokenGrpcService(IChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public override async Task<GetTokenResponse> GetTokenByChannelId(
        GetTokenByChannelIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ChannelId, out var channelId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Channel Id"));

        var (token, companyId) = await _channelRepository.GetTokenAndCompanyIdAsync(
            channelId,
            context.CancellationToken);

        if (token == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Token not found"));

        var response = new GetTokenResponse { Token = token };
        if (companyId.HasValue)
            response.CompanyId = companyId.Value.ToString();
        return response;
    }

    public override async Task<GetCompanyIdByChannelIdResponse> GetCompanyIdByChannelId(
        GetTokenByChannelIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ChannelId, out var channelId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Channel Id"));

        var (_, companyId) = await _channelRepository.GetTokenAndCompanyIdAsync(
            channelId,
            context.CancellationToken);

        var response = new GetCompanyIdByChannelIdResponse();
        if (companyId.HasValue)
            response.CompanyId = companyId.Value.ToString();
        return response;
    }
}
