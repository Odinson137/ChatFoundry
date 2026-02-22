using TelegramService.Interfaces;
using Workflow.Grpc;

namespace TelegramService.Services;

public sealed class GrpcBotTokenProvider(BotTokenService.BotTokenServiceClient client) : IBotTokenProvider
{
    public async Task<string> GetByChannelIdAsync(Guid channelId, CancellationToken ct)
    {
        var response = await client.GetTokenByChannelIdAsync(
            new GetTokenByChannelIdRequest { ChannelId = channelId.ToString() },
            cancellationToken: ct);
        return response.Token;
    }
}
