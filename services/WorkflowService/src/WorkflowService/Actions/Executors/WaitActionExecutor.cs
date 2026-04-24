using Scheduler.Grpc;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class WaitActionExecutor(
    ISessionRepository sessionRepository,
    IVariableService variableService,
    WorkflowGraphParser workflowGraphParser,
    WorkflowTextRenderer workflowTextRenderer,
    SchedulerGrpcService.SchedulerGrpcServiceClient schedulerClient)
    : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Wait;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not WaitNodeData waitData)
            throw new InvalidOperationException($"Node {node.Id} is not a Wait node");

        var durationStr = workflowTextRenderer.RenderText(waitData.Duration, session);
        if (!int.TryParse(durationStr, out var durationValue) || durationValue <= 0)
            throw new InvalidOperationException($"Invalid wait duration: {waitData.Duration}");

        var delay = waitData.Unit.ToLowerInvariant() switch
        {
            "seconds" => TimeSpan.FromSeconds(durationValue),
            "minutes" => TimeSpan.FromMinutes(durationValue),
            "hours" => TimeSpan.FromHours(durationValue),
            "days" => TimeSpan.FromDays(durationValue),
            _ => throw new InvalidOperationException($"Unknown duration unit: {waitData.Unit}")
        };

        var fireAt = DateTimeOffset.UtcNow + delay;
        var jobKey = $"wait:{action.Id}";

        var request = new ScheduleWaitJobRequest
        {
            JobKey = jobKey,
            FireAtUnixMs = fireAt.ToUnixTimeMilliseconds(),
            Channel = session.Channel.ToString(),
            ClientId = session.ClientId,
            CompanyId = session.Workflow.Bot.CompanyId.ToString(),
        };

        await schedulerClient.ScheduleWaitJobAsync(request, cancellationToken: ct);

        // Do NOT publish ActionCompletedEvent — the scheduler WaitJob will do it.
        // The action stays in Processing status until the timer fires.
    }
}
