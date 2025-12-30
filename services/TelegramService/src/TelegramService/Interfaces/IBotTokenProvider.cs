namespace TelegramService.Interfaces;

public interface IBotTokenProvider
{
    Task<string> GetByChatIdAsync(string chatId, CancellationToken ct);
    Task<string> GetByBotIdAsync(Guid botId, CancellationToken ct);
}
