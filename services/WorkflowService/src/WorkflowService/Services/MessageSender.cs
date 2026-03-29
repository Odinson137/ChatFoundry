using System.Data;
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
            messageKind = mediaData.MediaKind switch
            {
                MediaKind.Image => MessageKind.Photo,
                MediaKind.Video => MessageKind.Video,
                MediaKind.Audio => MessageKind.Audio,
                MediaKind.File => MessageKind.Document,
                _ => MessageKind.Photo
            };
            var fileId = string.IsNullOrEmpty(mediaData.Value)
                ? throw new NoNullAllowedException("File Id not presented")
                : workflowTextRenderer.RenderText(mediaData.Value, action.Session);
            var caption = string.IsNullOrEmpty(mediaData.Caption)
                ? null
                : workflowTextRenderer.RenderText(mediaData.Caption, action.Session);
            var payload = new MessagePayload(fileId, caption);
            messageJson = JsonConvert.SerializeObject(payload);
        }
        else
        {
            messageKind = MessageKindMapper.FromNodeType(node.Type);
            messageJson = workflowTextRenderer.RenderNodeText(node, action.Session, messageKind);
        }

        var companyId = workflow.Bot?.CompanyId;
        var channelId = session!.ChannelId;

        await producer.Produce(
            new BotOutgoingMessage(channelId, DefaultChannel.Telegram, message.ExternalUserId, messageJson, messageKind, companyId),
            ct
        );
    }
}