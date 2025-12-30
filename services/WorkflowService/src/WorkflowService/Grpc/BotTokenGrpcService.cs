using Grpc.Core;
using Workflow.Grpc;
using WorkflowService.Interfaces;

namespace WorkflowService.Grpc;

public sealed class BotTokenGrpcService : BotTokenService.BotTokenServiceBase
{
    private readonly IBotRepository _botRepository;
    private readonly ISessionRepository _sessionRepository;

    public BotTokenGrpcService(IBotRepository botService, ISessionRepository sessionRepository)
    {
        _botRepository = botService;
        _sessionRepository = sessionRepository;
    }

    public override async Task<GetTokenResponse> GetTokenByChatId(
        GetTokenByChatIdRequest request,
        ServerCallContext context)
    {
        var token = await _sessionRepository.GetBotTokenAsync(
            request.ClientId,
            context.CancellationToken);

        if (token == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Token not found"));

        return new GetTokenResponse { Token = token };
    }

    public override async Task<GetTokenResponse> GetTokenByBotId(
        GetTokenByBotIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.BotId, out Guid botId))
        {
            throw new InvalidCastException($"Invalid Bot Id: {request.BotId}");
        }
        
        var token = await _botRepository.GetBotTokenAsync(
            botId,
            context.CancellationToken);

        if (token == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Token not found"));

        return new GetTokenResponse { Token = token };
    }
}
