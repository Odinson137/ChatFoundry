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

            var node = workflowGraphParser.Parse(workflow.NodesDefinition, workflow.EdgesDefinition).GetStartNode(message.Source);

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

    public async Task CloseSessionAndHierarchyAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(sessionId, ct);
        if (session == null)
            return;

        var toClose = new List<Session> { session };

        await AddDescendantsAsync(sessionId, toClose, ct);

        var current = session;
        while (current.ParentSessionId is { } parentId)
        {
            var parent = await sessionRepository.GetAsync(parentId, ct);
            if (parent == null)
                break;
            toClose.Add(parent);
            current = parent;
        }

        foreach (var s in toClose)
        {
            if (s.Status == SessionStatus.Completed || s.Status == SessionStatus.Failed)
                continue;
            s.Status = SessionStatus.Failed;
            await sessionRepository.SaveAsync(s, ct);
        }
    }

    private async Task AddDescendantsAsync(Guid parentId, List<Session> list, CancellationToken ct)
    {
        var children = await sessionRepository.GetByParentSessionIdAsync(parentId, ct);
        foreach (var child in children)
        {
            list.Add(child);
            await AddDescendantsAsync(child.Id, list, ct);
        }
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
