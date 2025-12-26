using Confluent.Kafka;
using MassTransit;
using Shared.Application.Events;

namespace ClientHub.Consumers;

public class BotIncomingMessageConsumer
    : IConsumer<BotIncomingMessage>
{
    private readonly ClientHubDbContext _db;

    public BotIncomingMessageConsumer(ClientHubDbContext db)
    {
        _db = db;
    }

    public async Task Consume(
        ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;

        var channelType = Enum.Parse<ChannelType>(msg.ChannelType);

        // 1. Найти канал
        var clientChannel = await _db.ClientChannels
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x =>
                x.ChannelType == channelType &&
                x.ExternalUserId == msg.ExternalUserId);

        Client client;

        // 2. Если нет — создать клиента и канал
        if (clientChannel == null)
        {
            client = new Client(
                displayName: msg.Username ?? "Unknown");

            _db.Clients.Add(client);

            clientChannel = new ClientChannel(
                client.Id,
                channelType,
                msg.ExternalUserId,
                msg.Username,
                isPrimary: true);

            _db.ClientChannels.Add(clientChannel);
        }
        else
        {
            client = clientChannel.Client;
        }

        // 3. Найти или создать диалог
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(x =>
                x.ClientId == client.Id &&
                x.ChannelType == channelType &&
                x.Status == ConversationStatus.Open);

        if (conversation == null)
        {
            conversation = new Conversation(
                client.Id,
                channelType);

            _db.Conversations.Add(conversation);
        }

        // 4. Сохранить сообщение
        var message = new Message<,>(
            conversation.Id,
            MessageDirection.Inbound,
            msg.Text,
            msg.Payload);

        _db.Messages.Add(message);

        conversation.Touch();

        await _db.SaveChangesAsync();
    }
}
