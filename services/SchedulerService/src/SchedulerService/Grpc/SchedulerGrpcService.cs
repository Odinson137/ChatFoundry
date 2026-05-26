using Quartz;
using Scheduler.Grpc;
using SchedulerService.Jobs;

namespace SchedulerService.Grpc;

public class SchedulerGrpcServiceImpl(ISchedulerFactory schedulerFactory)
    : SchedulerGrpcService.SchedulerGrpcServiceBase
{
    public override async Task<ScheduleWaitJobResponse> ScheduleWaitJob(
        ScheduleWaitJobRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var scheduler = await schedulerFactory.GetScheduler(context.CancellationToken);

        var jobKey = new JobKey(request.JobKey, WaitJob.JobKeyPrefix);
        var fireAt = DateTimeOffset.FromUnixTimeMilliseconds(request.FireAtUnixMs);

        var jobDetail = JobBuilder.Create<WaitJob>()
            .WithIdentity(jobKey)
            .UsingJobData("channel", request.Channel)
            .UsingJobData("clientId", request.ClientId)
            .UsingJobData("companyId", request.CompanyId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(jobKey)
            .StartAt(fireAt.UtcDateTime)
            .WithSimpleSchedule(s => s.WithMisfireHandlingInstructionFireNow())
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger, context.CancellationToken);

        return new ScheduleWaitJobResponse { Success = true };
    }

    public override async Task<CancelWaitJobResponse> CancelWaitJob(
        CancelWaitJobRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var scheduler = await schedulerFactory.GetScheduler(context.CancellationToken);
        var jobKey = new JobKey(request.JobKey, WaitJob.JobKeyPrefix);

        var deleted = await scheduler.DeleteJob(jobKey, context.CancellationToken);

        return new CancelWaitJobResponse { Success = deleted };
    }

    public override async Task<RegisterTimerStartResponse> RegisterTimerStart(
        RegisterTimerStartRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var scheduler = await schedulerFactory.GetScheduler(context.CancellationToken);

        var jobKey = new JobKey(request.JobKey, TimerStartJob.JobKeyPrefix);

        var jobDetail = JobBuilder.Create<TimerStartJob>()
            .WithIdentity(jobKey)
            .UsingJobData("companyId", request.CompanyId)
            .UsingJobData("botId", request.BotId)
            .UsingJobData("channelId", request.ChannelId)
            .UsingJobData("channel", request.Channel)
            .UsingJobData("clientFilterJson", request.ClientFilterJson ?? "")
            .Build();

        ITrigger trigger;

        if (request.ScheduleType.Equals("Cron", StringComparison.OrdinalIgnoreCase))
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.Timezone);
            trigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithCronSchedule(request.CronExpression, x =>
                {
                    x.WithMisfireHandlingInstructionFireAndProceed();
                    x.InTimeZone(timeZone);
                })
                .Build();
        }
        else
        {
            var fireTime = DateTimeOffset.Parse(request.FireTimeUtc);
            trigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .StartAt(fireTime.UtcDateTime)
                .WithSimpleSchedule(s => s.WithMisfireHandlingInstructionFireNow())
                .Build();
        }

        await scheduler.ScheduleJob(jobDetail, trigger, context.CancellationToken);

        return new RegisterTimerStartResponse { Success = true };
    }

    public override async Task<UnregisterTimerStartResponse> UnregisterTimerStart(
        UnregisterTimerStartRequest request, global::Grpc.Core.ServerCallContext context)
    {
        var scheduler = await schedulerFactory.GetScheduler(context.CancellationToken);
        var jobKey = new JobKey(request.JobKey, TimerStartJob.JobKeyPrefix);

        var deleted = await scheduler.DeleteJob(jobKey, context.CancellationToken);

        return new UnregisterTimerStartResponse { Success = deleted };
    }
}
