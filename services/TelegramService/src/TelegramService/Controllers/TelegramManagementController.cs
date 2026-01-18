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
    [HttpPost("{botId:guid}/set-webhook")]
    public async Task<IActionResult> SetupWebhook(
        Guid botId, 
        [FromBody] SetWebhookRequest request)
    {
        logger.LogInformation("setup webhook");
        await producer.Produce(new TelegramSetWebhookEvent(botId, request.Token));
        return Accepted();
    }
}