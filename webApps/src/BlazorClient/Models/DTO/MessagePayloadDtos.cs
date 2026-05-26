using System.Text.Json.Serialization;

namespace BlazorClient.Models.DTO;

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

public class AskMessagePayloadDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("buttons")]
    public List<InlineButtonDto> Buttons { get; set; } = [];
}

public class InlineButtonDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("callbackData")]
    public string? CallbackData { get; set; }
}
