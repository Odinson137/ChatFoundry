using System.Text.Json;
using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class InputExecutor(
    ITopicProducer<ActionCompletedEvent> producer,
    ISessionRepository sessionRepository,
    IVariableService variableService,
    WorkflowGraphParser workflowGraphParser) : IActionExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.Input;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        var (output, error) = ParsePayload(action.Payload);

        variableService.SetVariable(session, $"$node.{node.Id}.output", output);
        variableService.SetVariable(session, $"$node.{node.Id}.error", error);
        await variableService.SyncIfDirtyAsync(session, ct);
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }

    private static (string Output, string Error) ParsePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return ("", "");

        var trimmed = payload.TrimStart();
        if (!trimmed.StartsWith("{"))
            return (payload, "");

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var error = root.TryGetProperty("error", out var errorProp)
                ? errorProp.GetString() ?? ""
                : "";

            var text = root.TryGetProperty("text", out var textProp)
                ? textProp.GetString() ?? ""
                : "";

            var output = !string.IsNullOrEmpty(text) ? text : payload;
            return (output, error);
        }
        catch
        {
            return (payload, "");
        }
    }
}
