using Shared.Domain.Models;

namespace TelegramService.Interfaces;

public interface ITelegramClient
{
    Task SendTextAsync(Guid channelId, string chatId, string text, CancellationToken ct);
    Task SetWebhookAsync(Guid channelId, string token, CancellationToken ct);
    Task SendInlineKeyboardAsync(Guid channelId, string chatId, string text, List<InlineButton> buttons, CancellationToken ct);
    Task SendMediaAsync(Guid channelId, string chatId, string value, string? caption, CancellationToken ct);
    Task SendDocumentAsync(Guid channelId, string chatId, string fileId, string? caption, CancellationToken ct);
    Task SendPhotoAsync(Guid channelId, string chatId, string photoUrl, string? caption, CancellationToken ct);
    Task SendVideoAsync(Guid channelId, string chatId, string videoUrl, string? caption, CancellationToken ct);
    Task SendAudioAsync(Guid channelId, string chatId, string audioUrl, string? caption, CancellationToken ct);
    Task<string?> GetFileAsync(Guid channelId, string chatId, string fileId, CancellationToken cancellationToken);
}

