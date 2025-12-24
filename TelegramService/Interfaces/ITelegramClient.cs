namespace TelegramService.Interfaces;

public interface ITelegramClient
{
    Task SendTextAsync(string chatId, string text, CancellationToken ct);
    Task SetWebhookAsync(string url, CancellationToken ct);
}