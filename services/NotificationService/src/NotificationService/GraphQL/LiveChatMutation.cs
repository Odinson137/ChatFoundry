using HotChocolate;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Entities;
using NotificationService.Enums;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using NotificationService.Services;
using Shared.Application.Events;
using Shared.Infrastructure.GraphQl;

namespace NotificationService.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class LiveChatMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<LiveChatSession> TakeLiveChatAsync(
        Guid liveChatSessionId,
        [Service] ILiveChatSessionRepository repository,
        [Service] LiveChatService liveChatService,
        [Service] IHubContext<LiveChatHub> hubContext,
        CancellationToken ct)
    {
        var session = await repository.GetWithIncludesAsync(liveChatSessionId, ct)
                      ?? throw new GraphQLException("Live chat session not found.");

        if (session.Status != LiveChatSessionStatus.Queued)
            throw new GraphQLException("Chat is not in queue.");

        session.Status = LiveChatSessionStatus.InProgress;
        session.OperatorId = UserId;
        session.TakenAt = DateTime.UtcNow;
        await repository.SaveAsync(session, ct);

        await liveChatService.SetRedisFlagAsync(session.ChannelId, session.ExternalUserId, session.Id, ct);

        if (session.CompanyId.HasValue)
        {
            await hubContext.Clients.Group(LiveChatHub.GetCompanyGroupName(session.CompanyId.Value))
                .SendAsync("ChatTaken", new { liveChatSessionId = session.Id }, ct);
        }

        return session;
    }

    public async Task<bool> SendLiveChatMessageAsync(
        Guid liveChatSessionId,
        string text,
        [Service] ILiveChatSessionRepository repository,
        [Service] LiveChatService liveChatService,
        [Service] ITopicProducer<BotOutgoingMessage> outgoingProducer,
        [Service] IHubContext<LiveChatHub> hubContext,
        CancellationToken ct)
    {
        var session = await repository.GetWithIncludesAsync(liveChatSessionId, ct)
                      ?? throw new GraphQLException("Live chat session not found.");

        if (session.Status != LiveChatSessionStatus.InProgress)
            throw new GraphQLException("Chat is not in progress.");

        if (session.OperatorId != UserId)
            throw new GraphQLException("You are not assigned to this chat.");

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(new { text });
        await outgoingProducer.Produce(new BotOutgoingMessage(
            session.ChannelId,
            session.Channel,
            session.ExternalUserId,
            payloadJson,
            Shared.Domain.Enums.MessageKind.Text,
            session.CompanyId), ct);

        if (session.CompanyId.HasValue)
        {
            await hubContext.Clients.Group(LiveChatHub.GetCompanyGroupName(session.CompanyId.Value))
                .SendAsync("MessageDelivered", new { liveChatSessionId = session.Id }, ct);
        }

        return true;
    }

    public async Task<bool> CloseLiveChatAsync(
        Guid liveChatSessionId,
        [Service] ILiveChatSessionRepository repository,
        [Service] LiveChatService liveChatService,
        [Service] ITopicProducer<ActionCompletedEvent> actionCompletedProducer,
        [Service] IHubContext<LiveChatHub> hubContext,
        CancellationToken ct)
    {
        var session = await repository.GetWithIncludesAsync(liveChatSessionId, ct)
                      ?? throw new GraphQLException("Live chat session not found.");

        if (session.Status != LiveChatSessionStatus.InProgress)
            throw new GraphQLException("Chat is not in progress.");

        session.Status = LiveChatSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        await repository.SaveAsync(session, ct);

        await liveChatService.RemoveRedisFlagAsync(session.ChannelId, session.ExternalUserId, ct);

        if (session.WorkflowSessionId.HasValue)
        {
            await actionCompletedProducer.Produce(new ActionCompletedEvent(
                session.Channel,
                session.ExternalUserId,
                session.CompanyId), ct);
        }

        if (session.CompanyId.HasValue)
        {
            await hubContext.Clients.Group(LiveChatHub.GetCompanyGroupName(session.CompanyId.Value))
                .SendAsync("ChatClosed", new { liveChatSessionId = session.Id }, ct);
        }

        return true;
    }

    public async Task<LiveChatSession> StartProactiveChatAsync(
        string externalUserId,
        Guid channelId,
        Shared.Domain.Enums.DefaultChannel channel,
        [Service] ILiveChatSessionRepository repository,
        [Service] LiveChatService liveChatService,
        [Service] IHubContext<LiveChatHub> hubContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new GraphQLException("externalUserId is required.");

        // Проверяем что чат ещё не существует
        var existing = await repository.GetActiveByChannelAndClientAsync(channelId, externalUserId, ct);
        if (existing != null)
            throw new GraphQLException("Live chat already exists for this client.");

        var session = new LiveChatSession
        {
            ExternalUserId = externalUserId,
            Channel = channel,
            ChannelId = channelId,
            CompanyId = CompanyId,
            Status = LiveChatSessionStatus.InProgress,
            OperatorId = UserId,
            TakenAt = DateTime.UtcNow
        };

        await repository.AddAsync(session, ct);
        await liveChatService.SetRedisFlagAsync(channelId, externalUserId, session.Id, ct);

        if (CompanyId.HasValue)
        {
            var group = LiveChatHub.GetCompanyGroupName(CompanyId.Value);
            await hubContext.Clients.Group(group)
                .SendAsync("ChatTaken", new { liveChatSessionId = session.Id }, ct);
            await hubContext.Clients.Group(group)
                .SendAsync("NewChatInQueue", new
                {
                    liveChatSessionId = session.Id,
                    externalUserId = session.ExternalUserId,
                    clientName = session.ClientFirstName ?? session.ClientUserName ?? session.ExternalUserId,
                    channel = session.Channel.ToString()
                }, ct);
        }

        return session;
    }
}
