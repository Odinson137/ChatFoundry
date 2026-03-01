using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class SessionResolver(
    ISessionRepository sessionRepository,
    IWorkflowRepository workflowRepository,
    IVariableService variableService,
    WorkflowGraphParser workflowGraphParser)
    : ISessionResolver
{
    public async Task<Session> ResolveForBotAsync(BotIncomingMessage message, Guid botId, CancellationToken ct)
    {
        var session = await sessionRepository.FindActiveAsync(message.ChannelId, message.ExternalUserId, botId, ct);
        if (session == null)
        {
            var workflow = await workflowRepository
                .GetActiveWorkflowAsync(botId, ct) ?? throw new InvalidOperationException($"Active workflow not found for bot {botId}.");

            var node = workflowGraphParser.Parse(workflow.NodesDefinition, workflow.EdgesDefinition).GetStartNode();

            session = new Session
            {
                Workflow = workflow,
                WorkflowId = workflow.Id,
                ClientId = message.ExternalUserId,
                Channel = message.Channel,
                ChannelId = message.ChannelId,
                CurrentNodeId = node.Id,
                Status = SessionStatus.Active,
            };

            PopulateDefaultInputParameters(session, workflow);

            await sessionRepository.AddAsync(session, ct);
        }

        session = await DrillDownToActiveChildAsync(session, ct);

        variableService.PopulateFromEventParameters(session, message.Parameters);
        await variableService.LoadClientVariablesAsync(session, ct);

        return session;
    }

    public async Task CloseSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(sessionId, ct);
        if (session == null)
            return;
        
        session.Status = SessionStatus.Completed;
        await sessionRepository.SaveAsync(session, ct);
    }

    private async Task<Session> DrillDownToActiveChildAsync(Session session, CancellationToken ct)
    {
        while (session.Status == SessionStatus.WaitingForSubWorkflow)
        {
            var child = await sessionRepository.FindActiveChildAsync(session.Id, ct);
            if (child == null)
                break;
            session = child;
        }

        return session;
    }

    private static void PopulateDefaultInputParameters(Session session, BotWorkflow workflow)
    {
        foreach (var param in workflow.InputParameters)
        {
            if (param.DefaultValue != null)
                session.Variables[param.Name] = param.DefaultValue;
        }
    }
}
