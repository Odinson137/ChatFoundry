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
            WorkflowNodeType.Image => MessageKind.Media,
            WorkflowNodeType.Video => MessageKind.Media,
            WorkflowNodeType.Audio => MessageKind.Media,
            WorkflowNodeType.Voice => MessageKind.Media,
            WorkflowNodeType.File => MessageKind.Media,
            WorkflowNodeType.Sticker => MessageKind.Media,
            WorkflowNodeType.Link => MessageKind.Link,
            WorkflowNodeType.Media => MessageKind.Media,
            WorkflowNodeType.Command => MessageKind.Command,
            WorkflowNodeType.Start => MessageKind.Unknown,
            WorkflowNodeType.End => MessageKind.Unknown,
            WorkflowNodeType.SubWorkflow => MessageKind.Command,
            _ => MessageKind.Unknown
        };
    }
}
