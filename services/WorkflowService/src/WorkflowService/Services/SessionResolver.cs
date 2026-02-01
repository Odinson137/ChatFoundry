using Grpc.Core;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class SessionResolver(
    ISessionRepository sessionRepository,
    IWorkflowRepository workflowRepository,
    WorkflowGraphParser workflowGraphParser)
    : ISessionResolver
{
    public async Task<Session> ResolveAsync(
        BotIncomingMessage message,
        CancellationToken ct)
    {
        var session = await sessionRepository.FindActiveAsync(message.ExternalUserId, message.Channel, ct);
        if (session == null)
        {
            var workflow = await workflowRepository
                .GetActiveWorkflowAsync(message.BotId, ct) ?? throw new InvalidOperationException("Active workflow not found.");

            var node = workflowGraphParser.Parse(workflow.NodesDefinition, workflow.EdgesDefinition).GetStartNode();
            
            session = new Session
            {
                Workflow = workflow,
                ClientId = message.ExternalUserId,
                Channel = message.Channel,
                CurrentNodeId = node.Id,
                Status = SessionStatus.Active,
            };

            await sessionRepository.AddAsync(session, ct);
        }
        
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
}
