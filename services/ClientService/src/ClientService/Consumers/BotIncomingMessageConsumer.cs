using ClientService.Entities;
using ClientService.Interfaces;
using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;

namespace ClientService.Consumers;

public class BotIncomingMessageConsumer(
    IClientRepository clientRepository,
    IClientChannelRepository channelRepository,
    IMessageRepository messageRepository) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var channel = msg.Channel;

        var clientChannel = await channelRepository
            .FindAsync(channel, msg.ExternalUserId, ct);

        var userName = msg.Parameters.GetValueOrDefault(MessageParameter.UserName);
        var mail = msg.Parameters.GetValueOrDefault(MessageParameter.Mail);
        var phone = msg.Parameters.GetValueOrDefault(MessageParameter.Phone);

        Client client;

        if (clientChannel == null)
        {
            client = new Client
            {
                DisplayName = userName
            };

            await clientRepository.AddAsync(client, ct);

            clientChannel = new ClientChannel
            {
                Client = client,
                Channel = channel,
                ExternalUserId = msg.ExternalUserId,
                Username = userName,
                Email = mail,
                Phone = phone
            };

            await channelRepository.AddAsync(clientChannel, ct);
        }
        else
        {
            client = clientChannel.Client;

            if (!string.IsNullOrWhiteSpace(userName) &&
                client.DisplayName != userName)
            {
                client.DisplayName = userName;
            }
        }

        var message = new Message
        {
            Direction = MessageDirection.Incoming,
            MessageKind = msg.MessageKind,
            Payload = msg.Payload,
            ClientChannel = clientChannel,
            InternalMessageId = msg.MessageExternalId
        };

        await messageRepository.AddAsync(message, ct);

        await clientRepository.SaveAsync(ct);
    }
}