using TelegramService.Interfaces;

namespace TelegramService.Services;

using Workflow.Grpc;

public sealed class GrpcBotTokenProvider(BotTokenService.BotTokenServiceClient client) : IBotTokenProvider
{
    public async Task<string> GetByChatIdAsync(string clientId, CancellationToken ct)
    {
        var response = await client.GetTokenByChatIdAsync(
            new GetTokenByClientIdRequest { ClientId = clientId },
            cancellationToken: ct);

        return response.Token;
    }

    public async Task<string> GetByBotIdAsync(Guid botId, CancellationToken ct)
    {
        var response = await client.GetTokenByBotIdAsync(
            new GetTokenByBotIdRequest { BotId = botId.ToString() },
            cancellationToken: ct);

        return response.Token;
    }
}
