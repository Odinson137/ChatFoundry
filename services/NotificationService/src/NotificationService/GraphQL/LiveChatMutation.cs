using HotChocolate;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Entities;
using NotificationService.Enums;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using NotificationService.Services;
using Shared.Application.Events;
using Shared.Domain.Enums;
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
        var session = await repository.TryTakeAsync(liveChatSessionId, UserId, ct)
                      ?? throw new GraphQLException("Chat is not in queue.");

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
        CancellationToken ct,
        MessageKind? messageKind = null,
        string? caption = null)
    {
        var session = await repository.GetWithIncludesAsync(liveChatSessionId, ct)
                      ?? throw new GraphQLException("Live chat session not found.");

        if (session.Status != LiveChatSessionStatus.InProgress)
            throw new GraphQLException("Chat is not in progress.");

        if (session.OperatorId != UserId)
            throw new GraphQLException("You are not assigned to this chat.");

        var kind = messageKind ?? MessageKind.Text;
        string payloadJson;
        if (kind == MessageKind.Text)
        {
            payloadJson = System.Text.Json.JsonSerializer.Serialize(new { text });
        }
        else
        {
            payloadJson = System.Text.Json.JsonSerializer.Serialize(new { text, caption });
        }

        await outgoingProducer.Produce(new BotOutgoingMessage(
            session.ChannelId,
            session.Channel,
            session.ExternalUserId,
            payloadJson,
            kind,
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
        Guid? channelClientId,
        DefaultChannel channel,
        [Service] ILiveChatSessionRepository repository,
        [Service] LiveChatService liveChatService,
        [Service] IClientAttributesService clientAttributesService,
        [Service] IHubContext<LiveChatHub> hubContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new GraphQLException("externalUserId is required.");

        var existing = await repository.GetActiveByChannelAndClientAsync(channelId, externalUserId, ct);
        if (existing != null)
            return existing;

        ClientChannelInfo? clientInfo = null;
        try
        {
            clientInfo = await clientAttributesService.GetClientChannelInfoAsync(
                externalUserId,
                channel.ToString(),
                channelId,
                ct);
        }
        catch
        {
            // ignored — fallback to no enrichment
        }

        var session = new LiveChatSession
        {
            ExternalUserId = externalUserId,
            Channel = channel,
            ChannelId = channelId,
            ClientChannelId = clientInfo?.Id ?? channelClientId,
            ClientFirstName = clientInfo?.Name,
            ClientUserName = clientInfo?.Username,
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
                    clientId = session.ExternalUserId,
                    clientName = session.ClientFirstName ?? session.ClientUserName ?? session.ExternalUserId,
                    channel = session.Channel.ToString(),
                    preview = session.LastMessagePreview
                }, ct);
        }

        return session;
    }
}
