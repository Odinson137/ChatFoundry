using ClientService.Entities;
using ClientService.Interfaces;
using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;

namespace ClientService.Consumers;

public class BotOutgoingMessageConsumer(
    IClientRepository clientRepository,
    IClientChannelRepository channelRepository,
    IMessageRepository messageRepository
) : IConsumer<BotOutgoingMessage>
{
    public async Task Consume(ConsumeContext<BotOutgoingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var clientChannel = await channelRepository.FindAsync(
            msg.Channel,
            msg.ExternalUserId,
            msg.CompanyId,
            ct
        );

        if (clientChannel == null)
        {
            throw new InvalidOperationException(
                $"ClientChannel not found. Channel={msg.Channel}, ExternalUserId={msg.ExternalUserId}"
            );
        }

        var message = new Message
        {
            Direction = MessageDirection.Outgoing,
            MessageKind = msg.MessageKind,
            Payload = msg.MessageJson,
            ClientChannel = clientChannel,
            InternalMessageId = context.CorrelationId.ToString() // kafka correlationId
        };

        await messageRepository.AddAsync(message, ct);
        await clientRepository.SaveAsync(ct);
    }
}
