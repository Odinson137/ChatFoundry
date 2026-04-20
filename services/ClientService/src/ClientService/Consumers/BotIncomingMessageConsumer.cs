using Billing.Grpc;
using ClientService.Entities;
using ClientService.Interfaces;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Application.Events;
using Shared.Domain.Enums;

namespace ClientService.Consumers;

public class BotIncomingMessageConsumer(
    IClientRepository clientRepository,
    IClientChannelRepository channelRepository,
    IMessageRepository messageRepository,
    IBotCompanyResolver botCompanyResolver,
    global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient billingClient,
    IConfiguration configuration,
    ILogger<BotIncomingMessageConsumer> logger) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var channel = msg.Channel;

        var companyId = msg.CompanyId ?? await botCompanyResolver.GetCompanyIdByChannelIdAsync(msg.ChannelId, ct);

        var clientChannel = await channelRepository
            .FindAsync(channel, msg.ExternalUserId, companyId, ct);

        var userName = msg.Parameters.GetValueOrDefault(MessageParameter.UserName);
        var name = msg.Parameters.GetValueOrDefault(MessageParameter.FirstName);
        var mail = msg.Parameters.GetValueOrDefault(MessageParameter.Mail);
        var phone = msg.Parameters.GetValueOrDefault(MessageParameter.Phone);

        Client client;

        if (clientChannel == null)
        {
            if (companyId.HasValue && configuration.GetValue("Billing:Enabled", true))
            {
                try
                {
                    var existingCount = await clientRepository.CountByCompanyAsync(companyId, ct);
                    var check = await billingClient.CheckQuotaAsync(new CheckQuotaRequest
                    {
                        CompanyId = companyId.Value.ToString("D"),
                        QuotaType = "clients",
                        ReportedUsage = existingCount
                    }, cancellationToken: ct);
                    if (!check.Allowed)
                        throw new InvalidOperationException(
                            $"Client quota exceeded. Limit {check.Limit}, current {check.Used}.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    logger.LogWarning(ex, "Billing unavailable; allowing new client");
                }
            }

            client = new Client
            {
                DisplayName = userName,
                CompanyId = companyId
            };

            await clientRepository.AddAsync(client, ct);

            clientChannel = new ClientChannel
            {
                Client = client,
                Channel = channel,
                ChannelId = msg.ChannelId,
                ExternalUserId = msg.ExternalUserId,
                Username = userName,
                Name = name,
                Email = mail,
                Phone = phone
            };

            await channelRepository.AddAsync(clientChannel, ct);

        }
        else
        {
            client = clientChannel.Client;
            
            if (clientChannel.ChannelId != msg.ChannelId)
                clientChannel.ChannelId = msg.ChannelId;

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