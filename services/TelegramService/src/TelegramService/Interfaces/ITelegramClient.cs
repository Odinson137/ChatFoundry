using Shared.Domain.Models;

namespace TelegramService.Interfaces;

public interface ITelegramClient
{
    Task SendTextAsync(string chatId, string text, CancellationToken ct);
    Task SetWebhookAsync(Guid botId, string url, CancellationToken ct);
    Task SendInlineKeyboardAsync(string chatId, string text, List<InlineButton> buttons, CancellationToken ct);
    Task SendDocumentAsync(string chatId, string fileId, CancellationToken ct);
    Task SendPhotoAsync(string chatId, string photoUrl, CancellationToken ct);
    Task SendVideoAsync(string chatId, string photoUrl, CancellationToken ct);
    Task<string?> GetFileAsync(string chatId, string fileId, CancellationToken cancellationToken);
}

