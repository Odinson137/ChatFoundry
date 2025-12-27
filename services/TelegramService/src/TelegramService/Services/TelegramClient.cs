using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramService.Interfaces;

namespace TelegramService.Services;

public sealed class TelegramClient(
    IConfiguration configuration,
    ILogger<TelegramClient> logger)
    : ITelegramClient
{
    public const string Token = "8206298582:AAHqEA_ULOItaGXxi0rAJt9fxztRmDtee2c"; 
    [field: AllowNull, MaybeNull]
    private TelegramBotClient BotClient
    {
        get
        {
            if (field == null)
                field = new TelegramBotClient(Token);
            return field;
        }
    }

    public async Task SendTextAsync(string chatId, string text, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        logger.LogInformation("Telegram message sent to {ChatId}", chatId);
    }

    public async Task SetWebhookAsync(string url, CancellationToken ct)
    {
        await BotClient.SetWebhook(
            url: url,
            maxConnections: 100,
            cancellationToken: ct);

        logger.LogInformation("Telegram webhook set: {Url}", url);
    }
}