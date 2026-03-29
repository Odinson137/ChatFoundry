using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Shared.Domain.Enums;
using Shared.Domain.Models;
using WorkflowService.Entities;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public partial class WorkflowTextRenderer(IVariableService variableService)
{
    private static readonly Regex VariableRegex = MyRegex();

    public string RenderNodeText(
        WorkflowNode node,
        Session session,
        MessageKind messageKind)
    {
        BotMessagePayload messagePayload = messageKind switch
        {
            MessageKind.Text or MessageKind.Link
                or MessageKind.Photo or MessageKind.Video or MessageKind.Audio
                or MessageKind.Voice or MessageKind.Document or MessageKind.Sticker
                => RenderMessagePayload(node, session),
            MessageKind.Buttons => RenderButtonsPayload(node, session),
            _ => throw new InvalidOperationException($"MessageKind '{messageKind}' is not supported by text renderer")
        };

        return JsonConvert.SerializeObject(messagePayload);
    }

    private MessagePayload RenderMessagePayload(WorkflowNode node, Session session)
    {
        if (node.Data is not MessageNodeData message)
            throw new InvalidOperationException($"Node {node.Id} does not contain text data");

        var text = RenderText(message.Text, session);
        return new MessagePayload(text);
    }

    private AskMessagePayload RenderButtonsPayload(WorkflowNode node, Session session)
    {
        if (node.Data is not AskNodeData ask)
            throw new InvalidOperationException($"Node {node.Id} does not contain ask data");

        var text = RenderText(ask.Text, session);
        var buttons = new List<InlineButton>();
        if (ask.Ui?.Buttons != null)
        {
            foreach (var b in ask.Ui.Buttons)
            {
                var renderedText = RenderText(b.Text ?? "", session);
                var renderedValue = RenderText(b.Value ?? "", session);
                var display = !string.IsNullOrWhiteSpace(renderedText)
                    ? renderedText.Trim()
                    : (renderedValue ?? "").Trim();
                if (string.IsNullOrEmpty(display))
                    continue;
                var callback = string.IsNullOrWhiteSpace(renderedValue) ? display : renderedValue.Trim();
                buttons.Add(new InlineButton(display, callback));
            }
        }

        return new AskMessagePayload(text, buttons);
    }

    public string RenderText(string text, Session session)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return VariableRegex.Replace(text, match =>
        {
            var name = match.Groups["name"].Value;
            var value = variableService.GetVariable(session, name);
            return value ?? string.Empty;
        });
    }

    [GeneratedRegex(@"\{\{(?<name>[\$a-zA-Z0-9_.-]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
