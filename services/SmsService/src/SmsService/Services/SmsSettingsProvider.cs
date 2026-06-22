using SmsService.Interfaces;
using Workflow.Grpc;

namespace SmsService.Services;

public sealed class SmsSettingsProvider(BotTokenService.BotTokenServiceClient client) : ISmsSettingsProvider
{
    public async Task<string> GetSenderPhoneByChannelIdAsync(Guid channelId, CancellationToken ct)
    {
        var response = await client.GetTokenByChannelIdAsync(
            new GetTokenByChannelIdRequest { ChannelId = channelId.ToString() },
            cancellationToken: ct);
        return response.Token;
    }
}
