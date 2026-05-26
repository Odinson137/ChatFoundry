using System.Text.Json;
using System.Text.Json.Serialization;
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
        var clientFilterJson = dataMap.GetString("clientFilterJson");

        var companyId = Guid.Parse(companyIdStr);

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
                                IgnoreCase = cond.IgnoreCase ?? false,
                            });
                        }
                    }

                    if (filter.Channels?.Count > 0)
                        filterRequest.Channels.AddRange(filter.Channels);

                    if (!string.IsNullOrEmpty(filter.Logic))
                        filterRequest.ConditionsLogic = filter.Logic;
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
        [JsonPropertyName("clientIds")] public List<string>? ClientIds { get; set; }

        [JsonPropertyName("attributeConditions")]
        public List<ClientAttributeFilterConditionDto>? AttributeConditions { get; set; }

        [JsonPropertyName("logic")] public string? Logic { get; set; }

        [JsonPropertyName("channels")] public List<int>? Channels { get; set; }
    }

    private sealed class ClientAttributeFilterConditionDto
    {
        [JsonPropertyName("attributeKey")] public string AttributeKey { get; set; } = "";
        [JsonPropertyName("operator")] public string Operator { get; set; } = "equals";
        [JsonPropertyName("value")] public string Value { get; set; } = "";
        [JsonPropertyName("ignoreCase")] public bool? IgnoreCase { get; set; }
    }
}