using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Enums;
using SmsService.Models;

namespace SmsService.Controllers;

[ApiController]
[Route("hook")]
public class SmsHookController(
    ITopicProducer<BotIncomingMessage> producer,
    ILogger<SmsHookController> logger)
    : ControllerBase
{
    [HttpPost("{channelId:guid}")]
    public async Task<IActionResult> ReceivedMessage([FromRoute] Guid channelId, [FromBody] InboundSmsWebhookDto body, CancellationToken token)
    {
        if (channelId == Guid.Empty || body == null)
        {
            logger.LogError("Invalid webhook request: body is null or channelId is empty");
            return BadRequest();
        }

        if (body.Event != "message.inbound")
        {
            logger.LogInformation("Ignored non-inbound event: {Event}", body.Event);
            return Ok();
        }

        var smsData = body.Data;
        if (smsData == null)
        {
            logger.LogError("Inbound message data is missing");
            return BadRequest();
        }

        logger.LogInformation("Received inbound SMS from {From} for channel {ChannelId}", smsData.From, channelId);

        var incomingMessage = new BotIncomingMessage(
            ChannelId: channelId,
            ExternalUserId: smsData.From,
            Channel: DefaultChannel.Sms,
            Payload: smsData.Body,
            MessageExternalId: smsData.MessageId,
            Parameters: new Dictionary<MessageParameter, string>
            {
                [MessageParameter.FirstName] = smsData.From,
                [MessageParameter.UserName] = smsData.From,
                [MessageParameter.Phone] = smsData.From
            }
        );

        await producer.Produce(incomingMessage, token);

        return Ok();
    }
}
