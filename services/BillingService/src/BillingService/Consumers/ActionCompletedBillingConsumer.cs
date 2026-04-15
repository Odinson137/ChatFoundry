using BillingService.Services;
using MassTransit;
using Shared.Application.Events;

namespace BillingService.Consumers;

public class ActionCompletedBillingConsumer(BillingAccountService account, ILogger<ActionCompletedBillingConsumer> logger)
    : IConsumer<ActionCompletedEvent>
{
    public async Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var msg = context.Message;
        if (!msg.CountAsAiWorkflowExecution || msg.CompanyId is null)
            return;

        try
        {
            await account.IncrementUsageAsync(
                msg.CompanyId.Value,
                BillingPlanConstants.QuotaAiExecutions,
                1,
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record AI usage for company {CompanyId}", msg.CompanyId);
        }
    }
}
