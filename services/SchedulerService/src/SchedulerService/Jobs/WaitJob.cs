using MassTransit;
using Quartz;
using Shared.Application.Events;
using Shared.Domain.Enums;

namespace SchedulerService.Jobs;

public class WaitJob(
    ITopicProducer<ActionCompletedEvent> actionCompletedProducer)
    : IJob
{
    public const string JobKeyPrefix = "wait";

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;

        var channel = Enum.Parse<DefaultChannel>(dataMap.GetString("channel")!);
        var clientId = dataMap.GetString("clientId")!;
        var companyIdStr = dataMap.GetString("companyId");
        Guid? companyId = string.IsNullOrEmpty(companyIdStr) ? null : Guid.Parse(companyIdStr);

        await actionCompletedProducer.Produce(
            new ActionCompletedEvent(channel, clientId, companyId),
            context.CancellationToken);
    }
}
