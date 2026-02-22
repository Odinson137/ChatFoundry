using Workflow.Grpc;

namespace ClientService.Services;

public sealed class BotCompanyResolver(BotTokenService.BotTokenServiceClient client) : Interfaces.IBotCompanyResolver
{
    public async Task<Guid?> GetCompanyIdByChannelIdAsync(Guid channelId, CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetCompanyIdByChannelIdAsync(
                new GetTokenByChannelIdRequest { ChannelId = channelId.ToString() },
                cancellationToken: ct);
            return string.IsNullOrEmpty(response.CompanyId) || !Guid.TryParse(response.CompanyId, out var id)
                ? null
                : id;
        }
        catch
        {
            return null;
        }
    }
}
