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
        
        var keyboardRows = buttons
            .Select(g => new KeyboardButton(g.Text))
            .ToArray();

        var markup = new ReplyKeyboardMarkup(keyboardRows);
        
        await client.SendMessage(
            chatId: clientid,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: ct);

        logger.LogInformation("Inline keyboard sent to {ChatId}", clientid);
    }

    public async Task SendDocumentAsync(string clientid, string fileId, CancellationToken ct)
    {
        var client = await InitializeClientAsync(clientid, ct);
        
        await client.SendDocument(
            chatId: clientid,
            document: fileId,
            cancellationToken: ct);

        logger.LogInformation("Document sent to {ChatId}", clientid);
    }

    public async Task SendPhotoAsync(string clientid, string photoUrl, CancellationToken ct)
    {
        var client = await InitializeClientAsync(clientid, ct);
        
        await client.SendPhoto(
            chatId: clientid,
            photo: photoUrl,
            cancellationToken: ct);

        logger.LogInformation("Photo sent to {ChatId}", clientid);
    }

    public async Task SendVideoAsync(string clientid, string videoUrl, CancellationToken ct)
    {
        var client = await InitializeClientAsync(clientid, ct);

        await client.SendVideo(
            chatId: clientid,
            video: videoUrl,
            cancellationToken: ct);

        logger.LogInformation("Video sent to {ChatId}", clientid);
    }

    public async Task<string?> GetFileAsync(string clientid, 
        string fileId, CancellationToken cancellationToken)
    {
        var client = await InitializeClientAsync(clientid, cancellationToken);
        
        var file = await client.GetFile(fileId, cancellationToken);
        return file.FilePath;
    }
}