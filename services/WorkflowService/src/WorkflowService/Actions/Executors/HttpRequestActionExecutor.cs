using System.Text;
using MassTransit;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class HttpRequestActionExecutor(
    IHttpClientFactory httpClientFactory,
    ISessionRepository sessionRepository,
    IVariableService variableService,
    ITopicProducer<ActionCompletedEvent> producer,
    WorkflowGraphParser workflowGraphParser,
    WorkflowTextRenderer workflowTextRenderer) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.HttpRequest;

    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
        var node = graph.GetNode(session.CurrentNodeId!.Value);

        if (node.Data is not HttpRequestNodeData requestData)
            return;

        var client = httpClientFactory.CreateClient();

        // Render all text fields with session variables
        var renderedUrl = workflowTextRenderer.RenderText(requestData.Url, session);
        var renderedBody = requestData.Body != null ? workflowTextRenderer.RenderText(requestData.Body, session) : null;
        var renderedHeaders = requestData.Headers.ToDictionary(
            kvp => kvp.Key,
            kvp => workflowTextRenderer.RenderText(kvp.Value, session)
        );

        var httpMethod = new HttpMethod(requestData.Method.ToUpper());
        using var requestMessage = new HttpRequestMessage(httpMethod, renderedUrl);

        if (renderedBody != null)
        {
            requestMessage.Content = new StringContent(renderedBody, Encoding.UTF8, "application/json");
        }

        foreach (var header in renderedHeaders)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // TODO доработать timeout
        var responseMessage = await client.SendAsync(requestMessage, ct);

        // TODO доработать размер файла, потому что гигабайты я брать не хочу
        var responseContent = await responseMessage.Content.ReadAsStringAsync(ct);
        
        variableService.SetVariable(session, $"$node.{node.Id}.output", responseContent);
        variableService.SetVariable(session, $"$node.{node.Id}.statusCode", (int)responseMessage.StatusCode);

        await variableService.SyncIfDirtyAsync(session, ct);
        await sessionRepository.SaveAsync(session, ct);

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }
}
