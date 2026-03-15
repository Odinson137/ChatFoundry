using System.Text.Json.Serialization;

namespace BlazorClient.Models.DTO;

/// <summary>
/// DTO for message payload: text or media (Text = content or file id, Caption = optional).
/// </summary>
public class MessagePayloadDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("telegram_file_id")]
    public string? TelegramFileId { get; set; }
}

/// <summary>
/// DTO for message with inline buttons.
/// </summary>
public class AskMessagePayloadDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("buttons")]
    public List<InlineButtonDto> Buttons { get; set; } = [];
}

/// <summary>
/// Single inline button (display text + callback data).
/// </summary>
public class InlineButtonDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("callbackData")]
    public string? CallbackData { get; set; }
}
