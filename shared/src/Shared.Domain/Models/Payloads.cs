namespace Shared.Domain.Models;

public abstract record BotMessagePayload;

public record MessagePayload(string Text, string? Caption = null) : BotMessagePayload;

public record AskMessagePayload(
    string Text, 
    List<InlineButton> Buttons
) : BotMessagePayload;

public record InlineButton(string Text, string CallbackData);
//
// public record LinkMessagePayload(
//     string Text // Link or File Id 
// ) : BotMessagePayload;

