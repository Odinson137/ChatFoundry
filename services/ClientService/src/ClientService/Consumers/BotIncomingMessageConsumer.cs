using ClientService.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Data;

namespace ClientService.Consumers;

public class BotIncomingMessageConsumer(ClientHubDbContext db) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;

        var channel = msg.Channel;

        var clientChannel = await db.ClientChannels
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x =>
                x.Channel == channel &&
                x.ExternalUserId == msg.ExternalUserId);

        Client client;

        var userName = msg.Parameters.GetValueOrDefault(MessageParameter.UserName);
        var mail = msg.Parameters.GetValueOrDefault(MessageParameter.Mail);
        var phone = msg.Parameters.GetValueOrDefault(MessageParameter.Phone);
        
        if (clientChannel == null)
        {
           
            client = new Client
            {
                DisplayName = userName
            };

            db.Clients.Add(client);

            clientChannel = new ClientChannel
            {
                Client = client,
                Channel = channel,
                ExternalUserId = msg.ExternalUserId,
                Username = userName,
                Email = mail,
                Phone = phone
            };

            db.ClientChannels.Add(clientChannel);
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
            MessageKind = MessageKind.Text,
            Payload = msg.Payload,
            CreatedBy = clientChannel,
            InternalMessageId = msg.MessageExternalId
        };

        db.Messages.Add(message);

        await db.SaveChangesAsync();
    }
}
