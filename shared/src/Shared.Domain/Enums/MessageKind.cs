namespace Shared.Domain.Enums;

public enum MessageKind
{
    Unknown = 0,
    Text = 1,
    Media = 2,
    Link = 3,
    Command = 5,
    CallbackQuery = 8,
    Buttons = 11,
}
