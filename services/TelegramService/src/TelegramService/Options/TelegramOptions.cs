namespace TelegramService.Options;

public class TelegramOptions
{
    public const string SectionName = "Telegram";
    
    public string WebhookUrl { get; set; } = string.Empty;
    public string SecretToken { get; set; } = string.Empty;
}