using Newtonsoft.Json;

namespace TelegramService.Models;

public sealed class TelegramUpdateDto
{
    [JsonProperty("update_id")] public long UpdateId { get; init; }

    [JsonProperty("message")] public TelegramMessageDto? Message { get; init; }
    [JsonProperty("callback_query")] public TelegramCallbackQueryDto? CallbackQuery { get; init; }

    [JsonProperty("edited_message")] public TelegramMessageDto? EditedMessage { get; init; }
}

public sealed class TelegramCallbackQueryDto
{
    [JsonProperty("id")] public string Id { get; init; } = null!;
    [JsonProperty("from")] public TelegramUserDto From { get; init; } = null!;
    [JsonProperty("message")] public TelegramMessageDto? Message { get; init; }

    [JsonProperty("inline_message_id")]
    public string? InlineMessageId { get; init; }

    [JsonProperty("chat_instance")] public string ChatInstance { get; init; } = null!;
    [JsonProperty("data")] public string? Data { get; init; }
}

public sealed class TelegramMessageDto
{
    [JsonProperty("message_id")] public long MessageId { get; init; }

    [JsonProperty("from")] public TelegramUserDto? From { get; init; }

    [JsonProperty("chat")] public TelegramChatDto Chat { get; init; } = null!;

    [JsonProperty("date")] public long Date { get; init; }

    [JsonProperty("text")] public string? Text { get; init; }

    [JsonProperty("entities")] public IReadOnlyList<TelegramMessageEntityDto>? Entities { get; init; }

    [JsonProperty("caption")] public string? Caption { get; init; }

    [JsonProperty("photo")] public IReadOnlyList<TelegramPhotoSizeDto>? Photo { get; init; }
    [JsonProperty("sticker")] public TelegramStickerDto? Sticker { get; init; }
    [JsonProperty("document")] public TelegramDocumentDto? Document { get; init; }
    [JsonProperty("voice")] public TelegramVoiceDto? Voice { get; init; }
    [JsonProperty("video")] public TelegramVideoDto? Video { get; init; }
    [JsonProperty("audio")] public TelegramAudioDto? Audio { get; init; }
}

public sealed class TelegramUserDto
{
    [JsonProperty("id")] public long Id { get; init; }

    [JsonProperty("is_bot")] public bool IsBot { get; init; }

    [JsonProperty("first_name")] public string FirstName { get; init; } = null!;

    [JsonProperty("username")] public string? Username { get; init; }

    [JsonProperty("language_code")] public string? LanguageCode { get; init; }
}

public sealed class TelegramChatDto
{
    [JsonProperty("id")] public long Id { get; init; }

    [JsonProperty("first_name")] public string? FirstName { get; init; }

    [JsonProperty("username")] public string? Username { get; init; }

    [JsonProperty("type")] public string Type { get; init; } = null!;
}

public sealed class TelegramMessageEntityDto
{
    [JsonProperty("offset")] public int Offset { get; init; }

    [JsonProperty("length")] public int Length { get; init; }

    [JsonProperty("type")] public string Type { get; init; } = null!;
}

public sealed class TelegramPhotoSizeDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("width")] public int Width { get; init; }
    [JsonProperty("height")] public int Height { get; init; }
    [JsonProperty("file_size")] public long? FileSize { get; init; }
}

public sealed class TelegramStickerDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("width")] public int Width { get; init; }
    [JsonProperty("height")] public int Height { get; init; }
    [JsonProperty("emoji")] public string? Emoji { get; init; }
}

public sealed class TelegramDocumentDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("file_name")] public string? FileName { get; init; }
    [JsonProperty("file_size")] public long? FileSize { get; init; }
    [JsonProperty("mime_type")] public string? MimeType { get; init; }
    [JsonProperty("thumb")] public TelegramPhotoSizeDto? Thumb { get; init; }
}

public sealed class TelegramVoiceDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("duration")] public int Duration { get; init; }
    [JsonProperty("mime_type")] public string? MimeType { get; init; }
    [JsonProperty("file_size")] public long? FileSize { get; init; }
}

public sealed class TelegramVideoDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("width")] public int Width { get; init; }
    [JsonProperty("height")] public int Height { get; init; }
    [JsonProperty("duration")] public int Duration { get; init; }
    [JsonProperty("file_name")] public string? FileName { get; init; }
    [JsonProperty("mime_type")] public string? MimeType { get; init; }
    [JsonProperty("file_size")] public long? FileSize { get; init; }
}

public sealed class TelegramAudioDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;
    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;
    [JsonProperty("duration")] public int Duration { get; init; }
    [JsonProperty("performer")] public string? Performer { get; init; }
    [JsonProperty("title")] public string? Title { get; init; }
    [JsonProperty("file_name")] public string? FileName { get; init; }
    [JsonProperty("mime_type")] public string? MimeType { get; init; }
    [JsonProperty("file_size")] public long? FileSize { get; init; }
}

public sealed class TelegramGetFileResponseDto
{
    [JsonProperty("ok")] public bool Ok { get; init; }

    [JsonProperty("result")] public TelegramFileDto Result { get; init; } = null!;
}

public sealed class TelegramFileDto
{
    [JsonProperty("file_id")] public string FileId { get; init; } = null!;

    [JsonProperty("file_unique_id")] public string FileUniqueId { get; init; } = null!;

    [JsonProperty("file_size")] public long FileSize { get; init; }

    [JsonProperty("file_path")] public string FilePath { get; init; } = null!;
}