using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Events;
using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Actions.Executors;

public class GetUserProfileActionExecutor(
    ISessionRepository sessionRepository,
    ITopicProducer<ActionCompletedEvent> producer,
    ClientAttributesService.ClientAttributesServiceClient clientAttributesServiceClient
) : IActionExecutor
{
    public WorkflowNodeType WorkflowNodeType => WorkflowNodeType.GetCurrentUserInfo;
    
    public async Task ExecuteAsync(ActionEntity action, ExecuteActionCommand message, CancellationToken ct)
    {
        var session = await sessionRepository.GetAsync(action.SessionId, ct);
        if (session == null)
            return;

        const string prefix = "user";

        var response = await clientAttributesServiceClient.GetClientAttributesAsync(
            new GetClientAttributesRequest
            {
                ExternalUserId = session.ClientId,
                Channel = session.Channel.ToString(),
            },
            cancellationToken: ct);

        session.SetVariable($"{prefix}.name", response.BaseAttributes.Name);
        session.SetVariable($"{prefix}.username", response.BaseAttributes.Username);
        session.SetVariable($"{prefix}.phone", response.BaseAttributes.Phone);
        session.SetVariable($"{prefix}.email", response.BaseAttributes.Email);

        foreach (var kvp in response.CustomAttributes)
        {
            session.SetVariable($"{prefix}.{kvp.Key}", kvp.Value);
        }

        await producer.Produce(new ActionCompletedEvent(message.Channel, message.ExternalUserId), ct);
    }

}