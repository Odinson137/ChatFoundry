using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using Shared.Application.Events;

namespace NotificationService.Consumers;

public class IncomingMessageConsumer(
    ILiveChatSessionRepository repository,
    IHubContext<LiveChatHub> hubContext) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var liveChat = await repository.GetActiveByChannelAndClientAsync(msg.ChannelId, msg.ExternalUserId, ct);
        if (liveChat == null)
            return;

        liveChat.LastMessagePreview = msg.Payload;
        liveChat.ModifiedAt = DateTime.UtcNow;
        
        await repository.SaveAsync(liveChat, ct);
        
        if (liveChat.CompanyId.HasValue)
        {
            await hubContext.Clients.Group(LiveChatHub.GetCompanyGroupName(liveChat.CompanyId.Value))
                .SendAsync("MessageReceived", new
                {
                    liveChatSessionId = liveChat.Id,
                    direction = "Client",
                    payload = msg.Payload,
                    messageKind = msg.MessageKind.ToString(),
                    timestamp = DateTime.UtcNow
                }, ct);
        }
    }
}
