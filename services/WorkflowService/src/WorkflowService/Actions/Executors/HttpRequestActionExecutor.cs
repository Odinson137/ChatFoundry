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
    WorkflowTextRenderer workflowTextRenderer,
    SsrfUrlValidator ssrfUrlValidator) : IActionExecutor
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

        var renderedUrl = workflowTextRenderer.RenderText(requestData.Url, session);

        if (!ssrfUrlValidator.IsUrlAllowed(renderedUrl, out var blockReason))
        {
            var errorMsg = $"Запрос к внутренним или запрещённым адресам не разрешён. {blockReason}";
            await SaveNodeResultAsync(session, node.Id, errorMsg, 403, false, ct);
            await CompleteOrThrowAsync(requestData, message, session, false, ct);
            return;
        }

        using var client = httpClientFactory.CreateClient();
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
        // TODO доработать размер файла, потому что гигабайты я брать не хочу
        HttpResponseMessage responseMessage;
        try
        {
            responseMessage = await client.SendAsync(requestMessage, ct);
        }
        catch (TaskCanceledException)
        {
            await SaveNodeResultAsync(session, node.Id, "Request timed out", 408, false, ct);
            await CompleteOrThrowAsync(requestData, message, session, false, ct);
            return;
        }
        catch (HttpRequestException ex)
        {
            await SaveNodeResultAsync(session, node.Id, ex.Message,
                (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), false, ct);
            await CompleteOrThrowAsync(requestData, message, session, false, ct);
            return;
        }

        var responseContent = await responseMessage.Content.ReadAsStringAsync(ct);
        var isSuccess = responseMessage.IsSuccessStatusCode;

        await SaveNodeResultAsync(session, node.Id, responseContent, (int)responseMessage.StatusCode, isSuccess, ct);
        await CompleteOrThrowAsync(requestData, message, session, isSuccess, ct);
    }

    private async Task SaveNodeResultAsync(
        Session session, Guid nodeId, object output, int statusCode, bool success,
        CancellationToken ct)
    {
        variableService.SetVariable(session, $"$node.{nodeId}.output", output);
        variableService.SetVariable(session, $"$node.{nodeId}.statusCode", statusCode);
        variableService.SetVariable(session, $"$node.{nodeId}.success", success);
        await variableService.SyncIfDirtyAsync(session, ct);
        await sessionRepository.SaveAsync(session, ct);
    }

    private async Task CompleteOrThrowAsync(
        HttpRequestNodeData requestData, ExecuteActionCommand message, Session session,
        bool success, CancellationToken ct)
    {
        if (!success && !requestData.ContinueOnError)
            throw new HttpRequestException("HTTP request failed and ContinueOnError is false");

        await producer.Produce(new ActionCompletedEvent(
            message.Channel, message.ExternalUserId,
            session.Workflow.Bot.CompanyId, Success: success), ct);
    }
}
