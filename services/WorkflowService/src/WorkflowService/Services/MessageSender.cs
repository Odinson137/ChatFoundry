using MassTransit;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Shared.Domain.Models;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class MessageSender(
    ITopicProducer<BotOutgoingMessage> producer,
    WorkflowGraphParser workflowGraphParser,
    IWorkflowRepository workflowRepository,
    ISessionRepository sessionRepository,
    WorkflowTextRenderer workflowTextRenderer,
    IFileUrlResolver fileUrlResolver
) : IMessageSender
{
    public async Task SendAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var workflow = await workflowRepository.GetActionWorkflowAsync(action.Id)
                       ?? throw new Exception("Workflow not found");

        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        var graph = workflowGraphParser.Parse(workflow.NodesDefinition, workflow.EdgesDefinition);
        var node = graph.GetNode(session!.CurrentNodeId!.Value);

        MessageKind messageKind;
        string messageJson;

        if (node.Type == WorkflowNodeType.Media && node.Data is MediaNodeData mediaData)
        {
            messageKind = MessageKindMapper.FromMediaKind(mediaData.MediaKind);
            var resolvedUrl = await ResolveMediaUrlAsync(mediaData.Value, ct);
            if (string.IsNullOrEmpty(resolvedUrl))
                throw new InvalidOperationException($"Could not resolve media URL for key or value: {mediaData.Value}");
            var caption = string.IsNullOrEmpty(mediaData.Caption)
                ? null
                : workflowTextRenderer.RenderText(mediaData.Caption, action.Session);
            var payload = new MessagePayload(resolvedUrl, caption);
            messageJson = JsonConvert.SerializeObject(payload);
        }
        else
        {
            messageKind = MessageKindMapper.FromNodeType(node.Type);
            messageJson = workflowTextRenderer.RenderNodeText(node, action.Session, messageKind);
        }

        await producer.Produce(
            new BotOutgoingMessage(DefaultChannel.Telegram, message.ExternalUserId, messageJson, messageKind),
            ct
        );
    }

    private async Task<string?> ResolveMediaUrlAsync(string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        return await fileUrlResolver.GetUrlAsync(trimmed, ct);
    }
}