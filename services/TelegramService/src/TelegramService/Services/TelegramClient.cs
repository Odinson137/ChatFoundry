using Microsoft.Extensions.Options;
using TelegramService.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramService.Interfaces;
using Shared.Domain.Models;

namespace TelegramService.Services;

public sealed class TelegramClient(
    IBotTokenProvider botTokenProvider,
    IFileSignedUrlProvider fileSignedUrlProvider,
    ILogger<TelegramClient> logger,
    IOptions<TelegramOptions> options,
    IConfiguration configuration)
    : ITelegramClient
{
    private async Task<ITelegramBotClient> GetClientAsync(Guid channelId, CancellationToken ct)
    {
        var token = await botTokenProvider.GetByChannelIdAsync(channelId, ct);
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException($"Token for channelId {channelId} not found");
        return new TelegramBotClient(token);
    }

    private static ITelegramBotClient GetClientFromToken(string token) => new TelegramBotClient(token);

    public async Task SendTextAsync(Guid channelId, string chatId, string text, CancellationToken ct)
    {
        var client = await GetClientAsync(channelId, ct);
        await client.SendMessage(chatId: chatId, text: text, cancellationToken: ct);
        logger.LogInformation("Telegram message sent to {ChatId}", chatId);
    }

    public Task SetWebhookAsync(Guid channelId, string token, CancellationToken ct)
    {
        var client = GetClientFromToken(token);
        var baseUrl = (configuration["Gateway:Url"] ?? string.Empty).TrimEnd('/');
        var url = $"{baseUrl}/telegram/hook/{channelId}";
        return client.SetWebhook(
            url,
            maxConnections: 40,
            secretToken: options.Value.SecretToken,
            cancellationToken: ct);
    }

    public async Task SendInlineKeyboardAsync(Guid channelId, string chatId, string text, List<InlineButton> buttons, CancellationToken ct)
    {
        var client = await GetClientAsync(channelId, ct);

        var valid = buttons
            .Select(b =>
            {
                var label = (b.Text ?? "").Trim();
                if (string.IsNullOrEmpty(label))
                    return null;
                var data = string.IsNullOrWhiteSpace(b.CallbackData)
                    ? label
                    : b.CallbackData.Trim();
                if (string.IsNullOrEmpty(data))
                    data = label;
                return InlineKeyboardButton.WithCallbackData(label, data);
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();

        if (valid.Length == 0)
        {
            await client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            logger.LogInformation("Telegram message sent without keyboard (no valid buttons) to {ChatId}", chatId);
            return;
        }

        var rows = valid.Select(b => new[] { b }).ToArray();
        var markup = new InlineKeyboardMarkup(rows);

        await client.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: ct);

        logger.LogInformation("Inline keyboard sent to {ChatId}", chatId);
    }

    private static string TruncateToUtf8Bytes(string value, int maxBytes)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        if (bytes.Length <= maxBytes) return value;
        var truncated = new byte[maxBytes];
        Array.Copy(bytes, truncated, maxBytes);
        return System.Text.Encoding.UTF8.GetString(truncated).TrimEnd('\uFFFD');
    }

    private async Task<(string Url, string Extension)> ResolveMediaAsync(string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (value ?? "", "");
        var trimmed = value.Trim();
        if (Guid.TryParse(trimmed, out var fileId))
        {
            var resolved = await fileSignedUrlProvider.GetSignedUrlAsync(fileId, ct);
            if (resolved != null)
                return (resolved.Url, resolved.Extension ?? "");
        }
        var ext = "";
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.AbsolutePath))
            ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return (trimmed, ext);
    }

    public async Task SendMediaAsync(Guid channelId, string chatId, string value, string? caption, CancellationToken ct)
    {
        var (url, extension) = await ResolveMediaAsync(value, ct);
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("SendMedia: empty url for chat {ChatId}", chatId);
            return;
        }
        var sendType = MediaExtensionMapping.GetSendTypeByExtension(extension);
        var client = await GetClientAsync(channelId, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        var captionOrNull = cap.Length > 0 ? cap : null;

        switch (sendType)
        {
            case MediaSendType.Photo:
                await TrySendPhotoOrFallbackAsync(client, chatId, url, captionOrNull, ct);
                break;
            case MediaSendType.Video:
                await TrySendVideoOrFallbackAsync(client, chatId, url, captionOrNull, ct);
                break;
            case MediaSendType.Audio:
                await TrySendAudioOrFallbackAsync(client, chatId, url, captionOrNull, ct);
                break;
            default:
                await TrySendDocumentOrFallbackAsync(client, chatId, url, captionOrNull, ct);
                break;
        }
    }

    private async Task TrySendPhotoOrFallbackAsync(ITelegramBotClient client, string chatId, string url, string? caption, CancellationToken ct)
    {
        try
        {
            await client.SendPhoto(chatId: chatId, photo: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Photo sent to {ChatId}", chatId);
        }
        catch
        {
            await client.SendDocument(chatId: chatId, document: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Photo sent as document to {ChatId}", chatId);
        }
    }

    private async Task TrySendVideoOrFallbackAsync(ITelegramBotClient client, string chatId, string url, string? caption, CancellationToken ct)
    {
        try
        {
            await client.SendVideo(chatId: chatId, video: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Video sent to {ChatId}", chatId);
        }
        catch
        {
            await client.SendDocument(chatId: chatId, document: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Video sent as document to {ChatId}", chatId);
        }
    }

    private async Task TrySendAudioOrFallbackAsync(ITelegramBotClient client, string chatId, string url, string? caption, CancellationToken ct)
    {
        try
        {
            await client.SendAudio(chatId: chatId, audio: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Audio sent to {ChatId}", chatId);
        }
        catch
        {
            await client.SendDocument(chatId: chatId, document: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Audio sent as document to {ChatId}", chatId);
        }
    }

    private async Task TrySendDocumentOrFallbackAsync(ITelegramBotClient client, string chatId, string url, string? caption, CancellationToken ct)
    {
        try
        {
            await client.SendDocument(chatId: chatId, document: url, caption: caption, cancellationToken: ct);
            logger.LogInformation("Document sent to {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document send failed for {ChatId}, sending as text", chatId);
            await client.SendMessage(chatId: chatId, text: !string.IsNullOrEmpty(caption) ? $"{url}\n{caption}" : url, cancellationToken: ct);
        }
    }

    public async Task SendDocumentAsync(Guid channelId, string chatId, string fileId, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(fileId, ct);
        var client = await GetClientAsync(channelId, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendDocument(chatId: chatId, document: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Document sent to {ChatId}", chatId);
    }

    public async Task SendPhotoAsync(Guid channelId, string chatId, string photoUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(photoUrl, ct);
        var client = await GetClientAsync(channelId, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendPhoto(chatId: chatId, photo: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Photo sent to {ChatId}", chatId);
    }

    public async Task SendVideoAsync(Guid channelId, string chatId, string videoUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(videoUrl, ct);
        var client = await GetClientAsync(channelId, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendVideo(chatId: chatId, video: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Video sent to {ChatId}", chatId);
    }

    public async Task SendAudioAsync(Guid channelId, string chatId, string audioUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(audioUrl, ct);
        var client = await GetClientAsync(channelId, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendAudio(chatId: chatId, audio: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Audio sent to {ChatId}", chatId);
    }

    public async Task<string?> GetFileAsync(Guid channelId, string chatId, string fileId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(channelId, cancellationToken);
        var file = await client.GetFile(fileId, cancellationToken);
        return file.FilePath;
    }
}