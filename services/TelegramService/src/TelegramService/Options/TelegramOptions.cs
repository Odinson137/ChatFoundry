namespace TelegramService.Options;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string SecretToken { get; set; } = string.Empty;
}