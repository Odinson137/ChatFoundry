namespace NotificationService.Interfaces;

public record ClientChannelInfo(Guid Id, string? Name, string? Username);

public interface IClientAttributesService
{
    Task<ClientChannelInfo?> GetClientChannelInfoAsync(
        string externalUserId,
        string channel,
        Guid? channelId,
        CancellationToken ct);
}
