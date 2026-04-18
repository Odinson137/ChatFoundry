using BillingService.Data;
using BillingService.Entities;
using BillingService.Enums;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Services;

public class BillingCycleService(
    IServiceProvider serviceProvider,
    ILogger<BillingCycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Billing cycle run failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var account = scope.ServiceProvider.GetRequiredService<BillingAccountService>();

        var now = DateTime.UtcNow;

        var due = await db.CompanySubscriptions
            .Include(s => s.Plan)
            .Include(s => s.PendingPlan)
            .Where(s => s.Status != SubscriptionStatus.Cancelled && s.CurrentPeriodEnd <= now)
            .ToListAsync(ct);

        foreach (var sub in due)
        {
            if (sub.Plan.PricePerMonth <= 0)
            {
                sub.CurrentPeriodStart = sub.CurrentPeriodEnd;
                sub.CurrentPeriodEnd = sub.CurrentPeriodEnd.AddMonths(1);

                ApplyPendingPlanChange(sub, logger);
                sub.ModifiedAt = now;
                await db.SaveChangesAsync(ct);
                continue;
            }

            var charged = await account.TryChargeMonthlyAsync(sub, ct);
            if (charged)
                continue;

            sub.Status = SubscriptionStatus.PastDue;
            sub.PastDueSince ??= now;
            sub.ModifiedAt = now;
            await db.SaveChangesAsync(ct);
        }

        var stalePastDue = await db.CompanySubscriptions
            .Where(s =>
                s.Status == SubscriptionStatus.PastDue &&
                s.PastDueSince != null &&
                s.PastDueSince <= now.AddDays(-7))
            .ToListAsync(ct);

        foreach (var sub in stalePastDue)
        {
            var freePlan = await db.SubscriptionPlans.FirstAsync(p => p.Slug == "free", ct);
            sub.PlanId = freePlan.Id;
            sub.PendingPlanId = null;
            sub.Status = SubscriptionStatus.Active;
            sub.PastDueSince = null;
            sub.CurrentPeriodStart = now;
            sub.CurrentPeriodEnd = now.AddMonths(1);
            sub.ModifiedAt = now;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Downgraded company {Company} to free after past due", sub.CompanyId);
        }
    }

    private static void ApplyPendingPlanChange(CompanySubscription sub, ILogger<BillingCycleService> logger)
    {
        if (!sub.PendingPlanId.HasValue)
            return;

        logger.LogInformation("Company {CompanyId} applying scheduled plan change to {Plan}",
            sub.CompanyId, sub.PendingPlan?.Slug ?? sub.PendingPlanId.Value.ToString());
        sub.PlanId = sub.PendingPlanId.Value;
        sub.PendingPlanId = null;
        sub.Status = SubscriptionStatus.Active;
        sub.PastDueSince = null;
    }
}
