using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Consumers;

public class ActionCompletedConsumer(
    IActionFactory actionFactory,
    IActionRepository actionRepository,
    ISessionRepository sessionRepository,
    ISessionResolver sessionResolver,
    IVariableService variableService,
    ITopicProducer<ExecuteActionCommand> producer,
    ITopicProducer<ActionCompletedEvent> actionCompletedProducer,
    WorkflowGraphParser workflowGraphParser)
    : IConsumer<ActionCompletedEvent>
{
    public async Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        Session? session = null;
        try
        {
            var lastUserAction = await actionRepository.GetAsync(msg.Channel, msg.ClientId, ct);
            if (lastUserAction == null)
                return;

            session = lastUserAction.Session;

            if (lastUserAction.Status != ActionStatus.Failed)
            {
                lastUserAction.MarkCompleted();
                await actionRepository.SaveAsync(lastUserAction, ct);
            }
            session.CompletedAt = DateTime.UtcNow;

            var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);

            var nextNode = graph.GetNextNode(lastUserAction.NodeId, session, variableService);
            if (nextNode == null)
            {
                await sessionResolver.CloseSessionAsync(session.Id, ct);

                if (session.ParentSessionId != null)
                    await ResumeParentSessionAsync(session, msg, ct);

                return;
            }

            if (lastUserAction.WorkflowNodeType == WorkflowNodeType.Ask)
                return;

            session.MoveTo(nextNode.Id);

            var nextAction = await actionFactory.CreateAsync(
                session,
                nextNode,
                nextNode.Type,
                cancellationToken: ct);

            await actionRepository.AddAsync(nextAction, ct);

            await producer.Produce(new ExecuteActionCommand(nextAction.Id, msg.ClientId, msg.Channel), ct);
        }
        catch
        {
            if (session != null)
                await sessionResolver.CloseSessionAndHierarchyAsync(session.Id, ct);
        }
    }

    private async Task ResumeParentSessionAsync(Session childSession, ActionCompletedEvent msg, CancellationToken ct)
    {
        var parentSession = await sessionRepository.GetAsync(childSession.ParentSessionId!.Value, ct);
        if (parentSession == null || parentSession.Status != SessionStatus.WaitingForSubWorkflow)
            return;

        var parentAction = await actionRepository.GetAsync(childSession.ParentActionId!.Value, ct);
        if (parentAction == null)
            return;

        var parentGraph = workflowGraphParser.Parse(
            parentSession.Workflow.NodesDefinition,
            parentSession.Workflow.EdgesDefinition);
        var subNode = parentGraph.GetNode(parentAction.NodeId);

        if (subNode.Data is SubWorkflowNodeData subData)
        {
            foreach (var (parentKey, childKey) in subData.OutputMappings)
            {
                if (string.IsNullOrEmpty(childKey) || string.IsNullOrEmpty(parentKey)) continue;
                
                var value = variableService.GetVariable(childSession, childKey);
                variableService.SetVariable(parentSession, parentKey, value);
            }
        }

        parentSession.Status = SessionStatus.Active;
        await sessionRepository.SaveAsync(parentSession, ct);

        await actionCompletedProducer.Produce(
            new ActionCompletedEvent(msg.Channel, msg.ClientId), ct);
    }
}