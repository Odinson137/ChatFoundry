using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Interfaces;

namespace WorkflowService.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController(
    ISessionRepository sessionRepository,
    IVariableService variableService,
    ITopicProducer<ActionCompletedEvent> producer) : ControllerBase
{
    [HttpPost("{botId:guid}/{clientId}")]
    [Authorize]
    public async Task<IActionResult> HandleWebhook(
        Guid botId,
        string clientId,
        [FromQuery] string channel,
        CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var channelStr = channel?.Trim() ?? string.Empty;

        if (!Enum.TryParse<DefaultChannel>(channelStr, true, out var parsedChannel))
        {
            return BadRequest(new { error = $"Invalid channel: {channel}" });
        }

        var session = await sessionRepository.FindWaitingForWebhookAsync(botId, clientId, parsedChannel, ct);
        if (session == null)
        {
            return NotFound(new { error = "No active session waiting for webhook found for the specified bot, client, and channel." });
        }

        if (session.CurrentNodeId == null)
        {
            return Conflict(new { error = "Session does not have a current node." });
        }

        var nodeId = session.CurrentNodeId.Value;

        variableService.SetVariable(session, $"$node.{nodeId}.output", body);
        variableService.SetVariable(session, "$webhook.payload", body);
        await variableService.SyncIfDirtyAsync(session, ct);

        session.Status = SessionStatus.Active;
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(
            session.Channel,
            session.ClientId,
            session.Workflow.Bot.CompanyId,
            Success: true
        ), ct);

        return Ok(new { status = "accepted", nodeId });
    }
}
