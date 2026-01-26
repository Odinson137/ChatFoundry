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

public class AIGenerateActionExecutor : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.AIGenerate;

    private readonly IOpenAiService _openAiService;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITopicProducer<ActionCompletedEvent> _producer;
    private readonly WorkflowGraphParser _workflowGraphParser;

    public AIGenerateActionExecutor(
        IOpenAiService openAiService, 
        ISessionRepository sessionRepository,
        ITopicProducer<ActionCompletedEvent> producer,
        WorkflowGraphParser workflowGraphParser)
    {
        _openAiService = openAiService;
        _sessionRepository = sessionRepository;
        _producer = producer;
        _workflowGraphParser = workflowGraphParser;
    }

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        // TODO можно попытаться вынести общую логику всех эксекьютеров в отдельный хэндлер
        var session = await _sessionRepository.GetAsync(action.SessionId, ct);
        if (session is null)
        {
            return;
        }

        var graph = _workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);
        // 
        
        if (node.Data is not AIGenerateNodeData aiData || string.IsNullOrWhiteSpace(aiData.Prompt))
        {
            await _producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
            return;
        }

        var resolvedPrompt = WorkflowTextRenderer.RenderText(aiData.Prompt, session);

        var result = await _openAiService.GetCompletionAsync(resolvedPrompt, ct);

        if (!string.IsNullOrWhiteSpace(aiData.Variable))
        {
            session.SetVariable(aiData.Variable, result);
            await _sessionRepository.SaveAsync(session, ct);
        }

        await _producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
