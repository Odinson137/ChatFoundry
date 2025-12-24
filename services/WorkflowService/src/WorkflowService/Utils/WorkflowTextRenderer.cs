using System.Text.RegularExpressions;
using WorkflowService.Entities;
using WorkflowService.Models.Node;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public partial class WorkflowTextRenderer
{
    private static readonly Regex VariableRegex =
        MyRegex();

    public string RenderNodeText(
        WorkflowNode node,
        Session session)
    {
        if (node.Data is not MessageNodeData message)
            throw new InvalidOperationException(
                $"Node {node.Id} does not contain message text");

        return RenderText(message.Text, session);
    }

    public string RenderText(
        string text,
        Session session)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return VariableRegex.Replace(text, match =>
        {
            var name = match.Groups["name"].Value;

            var value = session.GetVariable(name);

            return value ?? string.Empty;
        });
    }

    [GeneratedRegex(@"\{\{(?<name>[a-zA-Z0-9_.-]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}