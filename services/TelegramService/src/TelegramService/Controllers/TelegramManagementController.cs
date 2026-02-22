using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.DTO;
using Shared.Application.Events;

namespace TelegramService.Controllers;

[ApiController]
[Route("management")]
public class TelegramManagementController(
    ITopicProducer<TelegramSetWebhookEvent> producer,
    ILogger<TelegramManagementController> logger)
    : ControllerBase
{
    [HttpPost("{channelId:guid}/set-webhook")]
    public async Task<IActionResult> SetupWebhook(
        Guid channelId,
        [FromBody] SetWebhookRequest request)
    {
        logger.LogInformation("Setup webhook for channel {ChannelId}", channelId);
        await producer.Produce(new TelegramSetWebhookEvent(channelId, request.Token));
        return Accepted();
    }
}