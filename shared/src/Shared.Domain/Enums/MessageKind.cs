namespace Shared.Domain.Enums;

public enum MessageKind
{
    Unknown = 0,
    Text = 1,
    Image = 2,
    Link = 3,
    Audio = 4,
    Command = 5,
    Video = 6,
    Sticker = 7,
    CallbackQuery = 8,
    File = 9,
    Voice = 10,
    Buttons = 11,
}
