using MassTransit;
using Shared.Domain.Enums;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Exceptions;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class SubWorkflowActionExecutor(
    ISessionRepository sessionRepository,
    IWorkflowRepository workflowRepository,
    IActionFactory actionFactory,
    IActionRepository actionRepository,
    ITopicProducer<ExecuteActionCommand> producer,
    WorkflowGraphParser workflowGraphParser,
    WorkflowTextRenderer workflowTextRenderer) : IActionExecutor
{
    private const int MaxDepth = 10;

    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.SubWorkflow;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var parentSession = await sessionRepository.GetAsync(action.SessionId, ct)
                            ?? throw new InvalidOperationException($"Session {action.SessionId} not found");

        var graph = workflowGraphParser.Parse(
            parentSession.Workflow.NodesDefinition,
            parentSession.Workflow.EdgesDefinition);
        var node = graph.GetNode(parentSession.CurrentNodeId!.Value);

        if (node.Data is not SubWorkflowNodeData subData)
            throw new InvalidOperationException($"Node {node.Id} is not a SubWorkflow node");

        if (parentSession.Depth >= MaxDepth)
            throw new SubWorkflowDepthExceededException(MaxDepth);

        var childWorkflow = await workflowRepository.GetByIdAsync(subData.WorkflowId)
                            ?? throw new InvalidOperationException($"SubWorkflow target {subData.WorkflowId} not found");

        var childGraph = workflowGraphParser.Parse(
            childWorkflow.NodesDefinition,
            childWorkflow.EdgesDefinition);
        var startNode = childGraph.GetStartNode();

        var childSession = new Session
        {
            WorkflowId = childWorkflow.Id,
            Workflow = childWorkflow,
            ClientId = parentSession.ClientId,
            Channel = parentSession.Channel,
            ChannelId = parentSession.ChannelId,
            CurrentNodeId = startNode.Id,
            Status = SessionStatus.Active,
            ParentSessionId = parentSession.Id,
            ParentActionId = action.Id,
            Depth = parentSession.Depth + 1,
        };

        PopulateDefaultInputs(childSession, childWorkflow);
        MapInputVariables(childSession, subData, parentSession);

        await sessionRepository.AddAsync(childSession, ct);

        parentSession.Status = SessionStatus.WaitingForSubWorkflow;
        await sessionRepository.SaveAsync(parentSession, ct);

        var childAction = await actionFactory.CreateAsync(
            childSession, startNode, startNode.Type, cancellationToken: ct);
        await actionRepository.AddAsync(childAction, ct);

        await producer.Produce(
            new ExecuteActionCommand(childAction.Id, message.ExternalUserId, message.Channel), ct);
    }

    private static void PopulateDefaultInputs(Session childSession, BotWorkflow childWorkflow)
    {
        foreach (var param in childWorkflow.InputParameters)
        {
            if (param.DefaultValue != null)
                childSession.Variables[param.Name] = param.DefaultValue;
        }
    }

    private void MapInputVariables(Session childSession, SubWorkflowNodeData subData, Session parentSession)
    {
        foreach (var (childKey, expression) in subData.InputMappings)
        {
            var value = workflowTextRenderer.RenderText(expression, parentSession);
            childSession.Variables[childKey] = value;
        }
    }
}
