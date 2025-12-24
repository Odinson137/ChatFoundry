namespace Shared.Application.Events;

public sealed class TelegramSendMessageEvent
{
    public string ChatId { get; init; } = null!;
    public string Text { get; init; } = null!;
}