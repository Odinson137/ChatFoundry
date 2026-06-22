namespace SmsService.Interfaces;

public interface ISmsSettingsProvider
{
    Task<string> GetSenderPhoneByChannelIdAsync(Guid channelId, CancellationToken ct);
}
