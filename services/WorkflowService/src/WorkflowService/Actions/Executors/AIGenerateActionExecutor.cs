using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Services;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class AIGenerateActionExecutor(
    IOpenAiService openAiService,
    ISessionRepository sessionRepository,
    IVariableService variableService,
    ITopicProducer<ActionCompletedEvent> producer,
    WorkflowGraphParser workflowGraphParser,
    WorkflowTextRenderer workflowTextRenderer) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.AIGenerate;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session is null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not AIGenerateNodeData aiData || string.IsNullOrWhiteSpace(aiData.Prompt))
        {
            await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
            return;
        }

        var resolvedPrompt = workflowTextRenderer.RenderText(aiData.Prompt, session);

        var result = await openAiService.GetCompletionAsync(resolvedPrompt, ct);

        variableService.SetVariable(session, $"$node.{node.Id}.output", result);
        await variableService.SyncIfDirtyAsync(session, ct);
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
