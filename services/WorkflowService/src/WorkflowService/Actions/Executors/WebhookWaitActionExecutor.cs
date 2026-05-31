using Scheduler.Grpc;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;
using Shared.Domain.Enums;

namespace WorkflowService.Actions.Executors;

public class WebhookWaitActionExecutor(
    ISessionRepository sessionRepository,
    WorkflowGraphParser workflowGraphParser,
    SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient = null)
    : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.WebhookWait;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not WebhookWaitNodeData waitData)
            throw new InvalidOperationException($"Node {node.Id} is not a WebhookWait node");

        session.Status = SessionStatus.WaitingForWebhook;
        await sessionRepository.SaveAsync(session, ct);

        if (waitData.TimeoutSeconds > 0 && schedulerClient != null)
        {
            var fireAt = DateTimeOffset.UtcNow.AddSeconds(waitData.TimeoutSeconds);
            var jobKey = $"webhook-timeout:{action.Id}";

            var request = new ScheduleWaitJobRequest
            {
                JobKey = jobKey,
                FireAtUnixMs = fireAt.ToUnixTimeMilliseconds(),
                Channel = session.Channel.ToString(),
                ClientId = session.ClientId,
                CompanyId = session.Workflow.Bot.CompanyId.ToString(),
            };

            await schedulerClient.ScheduleWaitJobAsync(request, cancellationToken: ct);
        }
    }
}
