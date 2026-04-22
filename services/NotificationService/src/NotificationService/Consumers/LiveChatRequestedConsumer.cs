using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Entities;
using NotificationService.Enums;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using NotificationService.Services;
using Shared.Application.Events;

namespace NotificationService.Consumers;

public class LiveChatRequestedConsumer(
    ILiveChatSessionRepository repository,
    LiveChatService liveChatService,
    IClientAttributesService clientAttributesService,
    IHubContext<LiveChatHub> hubContext) : IConsumer<LiveChatRequestedEvent>
{
    public async Task Consume(ConsumeContext<LiveChatRequestedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        ClientChannelInfo? clientInfo = null;
        try
        {
            clientInfo = await clientAttributesService.GetClientChannelInfoAsync(
                evt.ExternalUserId,
                evt.Channel.ToString(),
                evt.ChannelId,
                ct);
        }
        catch
        {
            // ignored — fallback to event data
        }

        var liveChatSession = new LiveChatSession
        {
            WorkflowSessionId = evt.SessionId,
            ExternalUserId = evt.ExternalUserId,
            Channel = evt.Channel,
            ChannelId = evt.ChannelId,
            BotId = evt.BotId,
            BotName = evt.BotName,
            CompanyId = evt.CompanyId,
            ClientChannelId = clientInfo?.Id,
            ClientFirstName = clientInfo?.Name ?? evt.ClientFirstName,
            ClientUserName = clientInfo?.Username ?? evt.ClientUserName,
            LastMessagePreview = evt.LastMessagePreview,
            Status = LiveChatSessionStatus.Queued
        };

        await repository.AddAsync(liveChatSession, ct);
        await liveChatService.SetRedisFlagAsync(evt.ChannelId, evt.ExternalUserId, liveChatSession.Id, ct);

        if (evt.CompanyId.HasValue)
        {
            await hubContext.Clients.Group(LiveChatHub.GetCompanyGroupName(evt.CompanyId.Value))
                .SendAsync("NewChatInQueue", new
                {
                    liveChatSessionId = liveChatSession.Id,
                    clientId = evt.ExternalUserId,
                    clientName = liveChatSession.ClientFirstName ?? liveChatSession.ClientUserName ?? evt.ExternalUserId,
                    channel = evt.Channel.ToString().ToUpperInvariant(),
                    channelId = evt.ChannelId,
                    botName = evt.BotName,
                    preview = evt.LastMessagePreview
                }, ct);
        }
    }
}
