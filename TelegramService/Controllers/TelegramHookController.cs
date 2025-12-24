using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Constants;

namespace TelegramService.Controllers;

[ApiController]
[Route("telegramhook")]
public class TelegramHookController(ITopicProducer<BotIncomingMessage> producer, ILogger<TelegramHookController> logger)
    : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok();
    }
    
    [HttpPost("{BotId}")]
    public async Task<IActionResult> ReceivedMessage([FromRoute] Guid botId, [FromBody] TelegramUpdateDto body, CancellationToken token)
    {
        if (body.Message == null || string.IsNullOrEmpty(body.Message.Text) || botId == Guid.Empty)
        {
            logger.LogError($"{JsonConvert.SerializeObject(body)} is not a valid {botId}");
            return Ok();
        }
        
        await producer.Produce(new BotIncomingMessage(botId, body.Message.Chat.Id.ToString(), DefaultChannels.Telegram,  body.Message.Text), token);
        return Ok();
    }
}


public sealed class TelegramUpdateDto
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    [JsonPropertyName("message")]
    public TelegramMessageDto? Message { get; init; }
}

public sealed class TelegramMessageDto
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }

    [JsonPropertyName("from")]
    public TelegramUserDto? From { get; init; }

    [JsonPropertyName("chat")]
    public TelegramChatDto Chat { get; init; } = null!;

    [JsonPropertyName("date")]
    public long Date { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("entities")]
    public IReadOnlyList<TelegramMessageEntityDto>? Entities { get; init; }
}

public sealed class TelegramUserDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; init; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = null!;

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; init; }
}

public sealed class TelegramChatDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = null!;
}

public sealed class TelegramMessageEntityDto
{
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("length")]
    public int Length { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = null!;
}

