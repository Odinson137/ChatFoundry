using Shared.Domain.Enums;
using WorkflowService.Enums;

namespace WorkflowService.Utils;

public static class MessageKindMapper
{
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
            WorkflowNodeType.Link => MessageKind.Link, // не уверен нужно ли это

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
