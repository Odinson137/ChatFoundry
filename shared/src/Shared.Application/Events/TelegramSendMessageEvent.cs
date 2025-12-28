namespace Shared.Application.Events;

public record TelegramSendMessageEvent
{
    public string ChatId { get; init; } = null!;
    public string Text { get; init; } = null!;
}