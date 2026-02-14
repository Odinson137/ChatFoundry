using Shared.Domain.Enums;
using WorkflowService.Enums;
using WorkflowService.Models.Node;

namespace WorkflowService.Utils;

public static class MessageKindMapper
{
    public static MessageKind FromMediaKind(MediaKind mediaKind)
    {
        return mediaKind switch
        {
            MediaKind.Image => MessageKind.Image,
            MediaKind.Video => MessageKind.Video,
            MediaKind.Audio => MessageKind.Audio,
            MediaKind.File => MessageKind.File,
            _ => MessageKind.File
        };
    }

    public static MessageKind FromNodeType(WorkflowNodeType nodeType)
    {
        return nodeType switch
        {
            // ===== Textual =====
            WorkflowNodeType.Message => MessageKind.Text,
            WorkflowNodeType.Ask => MessageKind.Buttons,

            // ===== Media =====
            WorkflowNodeType.Image => MessageKind.Image,
            WorkflowNodeType.Video => MessageKind.Video,
            WorkflowNodeType.Audio => MessageKind.Audio,
            WorkflowNodeType.Voice => MessageKind.Voice,
            WorkflowNodeType.File => MessageKind.File,
            WorkflowNodeType.Sticker => MessageKind.Sticker,
            WorkflowNodeType.Link => MessageKind.Link,
            WorkflowNodeType.Media => MessageKind.Image, // фактический kind берётся из MediaNodeData в MessageSender

            // ===== System =====
            WorkflowNodeType.Command => MessageKind.Command,

            // ===== Flow-only =====
            WorkflowNodeType.Start => MessageKind.Unknown,
            WorkflowNodeType.End => MessageKind.Unknown,
            WorkflowNodeType.SubWorkflow => MessageKind.Command,

            _ => MessageKind.Unknown
        };
    }
}
