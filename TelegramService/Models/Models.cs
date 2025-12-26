using System.Text.Json.Serialization;
using TelegramService.Controllers;

namespace TelegramService.Models;

public sealed class TelegramUpdateDto
{
    [JsonPropertyName("update_id")] public long UpdateId { get; init; }

    [JsonPropertyName("message")] public TelegramMessageDto? Message { get; init; }
    [JsonPropertyName("callback_query")] public TelegramCallbackQueryDto? CallbackQuery { get; init; }
    [JsonPropertyName("edited_message")] public TelegramMessageDto? EditedMessage { get; init; }
    // Добавьте при необходимости: inline_query, chosen_inline_result и т.д.
}


public sealed class TelegramCallbackQueryDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = null!;
    [JsonPropertyName("from")] public TelegramUserDto From { get; init; } = null!;
    [JsonPropertyName("message")] public TelegramMessageDto? Message { get; init; }
    [JsonPropertyName("inline_message_id")] public string? InlineMessageId { get; init; }
    [JsonPropertyName("chat_instance")] public string ChatInstance { get; init; } = null!;
    [JsonPropertyName("data")] public string? Data { get; init; }
}

public sealed class TelegramMessageDto
{
     [JsonPropertyName("message_id")] public long MessageId { get; init; }

     [JsonPropertyName("from")] public TelegramUserDto? From { get; init; }

     [JsonPropertyName("chat")] public TelegramChatDto Chat { get; init; } = null!;

     [JsonPropertyName("date")] public long Date { get; init; }

     [JsonPropertyName("text")] public string? Text { get; init; }

     [JsonPropertyName("entities")] public IReadOnlyList<TelegramMessageEntityDto>? Entities { get; init; }
     
    [JsonPropertyName("photo")] public IReadOnlyList<TelegramPhotoSizeDto>? Photo { get; init; }
    [JsonPropertyName("sticker")] public TelegramStickerDto? Sticker { get; init; }
    //[JsonPropertyName("document")] public TelegramDocumentDto? Document { get; init; }
}

public sealed class TelegramUserDto
{
    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("is_bot")] public bool IsBot { get; init; }

    [JsonPropertyName("first_name")] public string FirstName { get; init; } = null!;

    [JsonPropertyName("username")] public string? Username { get; init; }

    [JsonPropertyName("language_code")] public string? LanguageCode { get; init; }
}

public sealed class TelegramChatDto
{
    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("first_name")] public string? FirstName { get; init; }

    [JsonPropertyName("username")] public string? Username { get; init; }

    [JsonPropertyName("type")] public string Type { get; init; } = null!;
}

public sealed class TelegramMessageEntityDto
{
    [JsonPropertyName("offset")] public int Offset { get; init; }

    [JsonPropertyName("length")] public int Length { get; init; }

    [JsonPropertyName("type")] public string Type { get; init; } = null!;
}

public sealed class TelegramPhotoSizeDto
{
    [JsonPropertyName("file_id")] public string FileId { get; init; } = null!;
    [JsonPropertyName("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonPropertyName("width")] public int Width { get; init; }
    [JsonPropertyName("height")] public int Height { get; init; }
    [JsonPropertyName("file_size")] public long? FileSize { get; init; }
}

public sealed class TelegramStickerDto
{
    [JsonPropertyName("file_id")] public string FileId { get; init; } = null!;
    [JsonPropertyName("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonPropertyName("width")] public int Width { get; init; }
    [JsonPropertyName("height")] public int Height { get; init; }
    [JsonPropertyName("emoji")] public string? Emoji { get; init; }
}
