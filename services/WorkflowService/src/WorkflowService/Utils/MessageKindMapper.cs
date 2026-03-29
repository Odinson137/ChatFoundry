using Shared.Domain.Enums;
using WorkflowService.Enums;

namespace WorkflowService.Utils;

public static class MessageKindMapper
{
    public static MessageKind FromNodeType(WorkflowNodeType nodeType)
    {
        return nodeType switch
        {
            WorkflowNodeType.Message => MessageKind.Text,
            WorkflowNodeType.Ask => MessageKind.Buttons,
            WorkflowNodeType.Image => MessageKind.Photo,
            WorkflowNodeType.Video => MessageKind.Video,
            WorkflowNodeType.Audio => MessageKind.Audio,
            WorkflowNodeType.Voice => MessageKind.Voice,
            WorkflowNodeType.File => MessageKind.Document,
            WorkflowNodeType.Sticker => MessageKind.Sticker,
            WorkflowNodeType.Link => MessageKind.Link,
            WorkflowNodeType.Media => MessageKind.Photo,
            WorkflowNodeType.Command => MessageKind.Command,
            WorkflowNodeType.Start => MessageKind.Unknown,
            WorkflowNodeType.SubWorkflow => MessageKind.Command,
            _ => MessageKind.Unknown
        };
    }
}
