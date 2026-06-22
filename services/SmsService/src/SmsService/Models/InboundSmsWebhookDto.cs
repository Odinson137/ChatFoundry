using Newtonsoft.Json;

namespace SmsService.Models;

public class InboundSmsWebhookDto
{
    [JsonProperty("event")]
    public string Event { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonProperty("data")]
    public InboundSmsDataDto Data { get; set; } = null!;
}

public class InboundSmsDataDto
{
    [JsonProperty("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonProperty("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonProperty("from")]
    public string From { get; set; } = string.Empty;

    [JsonProperty("to")]
    public string To { get; set; } = string.Empty;

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonProperty("simSlot")]
    public int SimSlot { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
}
