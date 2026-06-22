using Newtonsoft.Json;

namespace SmsService.Models;

public class SendSmsRequestDto
{
    [JsonProperty("to")]
    public string To { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("from")]
    public string From { get; set; } = string.Empty;

    [JsonProperty("channel")]
    public string Channel { get; set; } = "sms";

    [JsonProperty("externalId")]
    public string? ExternalId { get; set; }
}
