using System.Text.Json;
using MassTransit;
using Quartz;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Workflow.Grpc.Client;

namespace SchedulerService.Jobs;

public class TimerStartJob(
    ClientAttributesService.ClientAttributesServiceClient clientServiceClient,
    ITopicProducer<BotIncomingMessage> botMessageProducer)
    : IJob
{
    public const string JobKeyPrefix = "timer";

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;

        var companyIdStr = dataMap.GetString("companyId")!;
        // var botIdStr = dataMap.GetString("botId")!;
        // var channelIdStr = dataMap.GetString("channelId")!;
        // var channel = (DefaultChannel)dataMap.GetInt("channel");
        var clientFilterJson = dataMap.GetString("clientFilterJson");

        Guid companyId = Guid.Parse(companyIdStr);

        // Query ClientService for matching clients
        var filterRequest = new GetClientsByFilterRequest
        {
            CompanyId = companyIdStr,
        };

        if (!string.IsNullOrEmpty(clientFilterJson))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<ClientFilterDto>(clientFilterJson);
                if (filter != null)
                {
                    if (filter.ClientIds?.Count > 0)
                        filterRequest.ClientIds.AddRange(filter.ClientIds);

                    if (filter.AttributeConditions?.Count > 0)
                    {
                        foreach (var cond in filter.AttributeConditions)
                        {
                            filterRequest.AttributeConditions.Add(new ClientAttributeFilterCondition
                            {
                                AttributeKey = cond.AttributeKey,
                                Operator = cond.Operator,
                                Value = cond.Value,
                            });
                        }
                    }

                    if (filter.Channels?.Count > 0)
                        filterRequest.Channels.AddRange(filter.Channels);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize client filter: {ex.Message}");
                return;
            }
        }

        GetClientsByFilterResponse filterResponse;
        try
        {
            filterResponse = await clientServiceClient.GetClientsByFilterAsync(filterRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to query clients by filter: {ex.Message}");
            return;
        }

        // Publish BotIncomingMessage for each matching client
        foreach (var client in filterResponse.Clients)
        {
            var message = new BotIncomingMessage(
                Guid.Parse(client.ChannelId),
                client.ExternalUserId,
                (DefaultChannel)client.Channel,
                Payload: "",
                MessageExternalId: Guid.NewGuid().ToString(),
                Parameters: new Dictionary<MessageParameter, string>(),
                MessageKind: MessageKind.Text,
                CompanyId: companyId,
                Source: BotIncomingMessageSource.Timer);

            await botMessageProducer.Produce(message, context.CancellationToken);
        }
    }

    private sealed class ClientFilterDto
    {
        public List<string>? ClientIds { get; set; }
        public List<ClientAttributeFilterConditionDto>? AttributeConditions { get; set; }
        public List<int>? Channels { get; set; }
    }

    private sealed class ClientAttributeFilterConditionDto
    {
        public string AttributeKey { get; set; } = "";
        public string Operator { get; set; } = "equals";
        public string Value { get; set; } = "";
    }
}
