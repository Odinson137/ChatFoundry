using Shared.Domain.Models;

namespace TelegramService.Interfaces;

public interface ITelegramClient
{
    Task SendTextAsync(string chatId, string text, CancellationToken ct);
    Task SetWebhookAsync(Guid botId, string url, CancellationToken ct);
    Task SendInlineKeyboardAsync(string chatId, string text, List<InlineButton> buttons, CancellationToken ct);
    Task SendMediaAsync(string chatId, string value, string? caption, CancellationToken ct);
    Task SendDocumentAsync(string chatId, string fileId, string? caption, CancellationToken ct);
    Task SendPhotoAsync(string chatId, string photoUrl, string? caption, CancellationToken ct);
    Task SendVideoAsync(string chatId, string videoUrl, string? caption, CancellationToken ct);
    Task SendAudioAsync(string chatId, string audioUrl, string? caption, CancellationToken ct);
    Task<string?> GetFileAsync(string chatId, string fileId, CancellationToken cancellationToken);
}

