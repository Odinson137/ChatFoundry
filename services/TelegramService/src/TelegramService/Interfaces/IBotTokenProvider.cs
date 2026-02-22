namespace TelegramService.Interfaces;

public interface IBotTokenProvider
{
    Task<string> GetByChannelIdAsync(Guid channelId, CancellationToken ct);
}
