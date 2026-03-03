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
            MessageKind.Text or MessageKind.Link or MessageKind.Media => RenderMessagePayload(node, session),
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
        var buttons = ask.Ui?.Buttons.Select(b => new InlineButton(
            RenderText(b.Text, session),
            RenderText(b.Value, session)
        )).ToList() ?? [];

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
