using Microsoft.Extensions.Options;
using TelegramService.Options;
using System.Diagnostics.CodeAnalysis;
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
    IOptions<TelegramOptions> options)
    : ITelegramClient
{
    private ITelegramBotClient? _botClient;
    
    private async Task<ITelegramBotClient> InitializeClientAsync(string clientId, CancellationToken ct)
    {
        if (_botClient == null)
        {
            var token= await botTokenProvider.GetByChatIdAsync(clientId, ct);
            if (token == null)
            {
                throw new InvalidOperationException($"Token for clientId {clientId} not found");
            }
            _botClient = new TelegramBotClient(token);
        }

        return _botClient;
    }

    private async Task<ITelegramBotClient> InitializeClientByBotIdAsync(Guid botId, CancellationToken ct)
    {
        if (_botClient != null) return _botClient;
        
        var token= await botTokenProvider.GetByBotIdAsync(botId, ct);
        if (token == null)
        {
            throw new InvalidOperationException($"Token for clientId {botId} not found");
        }
        
        _botClient = new TelegramBotClient(token);
        return _botClient;
    }
    
    private ITelegramBotClient InitializeClientByBotIdAsync(string token)
    {
        _botClient = new TelegramBotClient(token);
        return _botClient;
    }
    
    public async Task SendTextAsync(string clientid, string text, CancellationToken ct)
    {
        var client = await InitializeClientAsync(clientid, ct);
        
        await client.SendMessage(
            chatId: clientid,
            text: text,
            cancellationToken: ct);

        logger.LogInformation("Telegram message sent to {ChatId}", clientid);
    }

    public async Task SetWebhookAsync(Guid botId, string token, CancellationToken ct)
    {
        var client = InitializeClientByBotIdAsync(token);

        var url = $"{options.Value.WebhookUrl}/telegram/hook/{botId}";
        await client.SetWebhook(
            url,
            maxConnections: 40, // default
            secretToken: options.Value.SecretToken,
            cancellationToken: ct);

        logger.LogInformation("Telegram webhook set: {Url}", url);
    }

    public async Task SendInlineKeyboardAsync(string clientid, string text, List<InlineButton> buttons, CancellationToken ct)
    {
        var client = await InitializeClientAsync(clientid, ct);

        var rows = buttons
            .Select(b =>
            {
                return new[] { InlineKeyboardButton.WithCallbackData(b.Text, string.IsNullOrWhiteSpace(b.CallbackData) ? b.Text :  b.CallbackData)
                };
            })
            .ToArray();

        var markup = new InlineKeyboardMarkup(rows);

        await client.SendMessage(
            chatId: clientid,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: ct);

        logger.LogInformation("Inline keyboard (attached to message) sent to {ChatId}", clientid);
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

    public async Task SendMediaAsync(string clientid, string value, string? caption, CancellationToken ct)
    {
        var (url, extension) = await ResolveMediaAsync(value, ct);
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("SendMedia: empty url for chat {ChatId}", clientid);
            return;
        }
        var sendType = MediaExtensionMapping.GetSendTypeByExtension(extension);
        var client = await InitializeClientAsync(clientid, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        var captionOrNull = cap.Length > 0 ? cap : null;

        switch (sendType)
        {
            case MediaSendType.Photo:
                await TrySendPhotoOrFallbackAsync(client, clientid, url, captionOrNull, ct);
                break;
            case MediaSendType.Video:
                await TrySendVideoOrFallbackAsync(client, clientid, url, captionOrNull, ct);
                break;
            case MediaSendType.Audio:
                await TrySendAudioOrFallbackAsync(client, clientid, url, captionOrNull, ct);
                break;
            default:
                await TrySendDocumentOrFallbackAsync(client, clientid, url, captionOrNull, ct);
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

    public async Task SendDocumentAsync(string clientid, string fileId, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(fileId, ct);
        var client = await InitializeClientAsync(clientid, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendDocument(chatId: clientid, document: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Document sent to {ChatId}", clientid);
    }

    public async Task SendPhotoAsync(string clientid, string photoUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(photoUrl, ct);
        var client = await InitializeClientAsync(clientid, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendPhoto(chatId: clientid, photo: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Photo sent to {ChatId}", clientid);
    }

    public async Task SendVideoAsync(string clientid, string videoUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(videoUrl, ct);
        var client = await InitializeClientAsync(clientid, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendVideo(chatId: clientid, video: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Video sent to {ChatId}", clientid);
    }

    public async Task SendAudioAsync(string clientid, string audioUrl, string? caption, CancellationToken ct)
    {
        var (url, _) = await ResolveMediaAsync(audioUrl, ct);
        var client = await InitializeClientAsync(clientid, ct);
        var cap = TruncateToUtf8Bytes(caption ?? "", 1024);
        await client.SendAudio(chatId: clientid, audio: url, caption: cap.Length > 0 ? cap : null, cancellationToken: ct);
        logger.LogInformation("Audio sent to {ChatId}", clientid);
    }

    public async Task<string?> GetFileAsync(string clientid, 
        string fileId, CancellationToken cancellationToken)
    {
        var client = await InitializeClientAsync(clientid, cancellationToken);
        
        var file = await client.GetFile(fileId, cancellationToken);
        return file.FilePath;
    }
}