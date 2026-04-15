using BillingService.Data;
using BillingService.Entities;
using BillingService.Enums;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Services;

public class BillingAccountService(
    BillingDbContext db,
    ILogger<BillingAccountService> logger)
{
    public static (DateTime Start, DateTime End) GetCurrentUsagePeriodUtc(DateTime utcNow)
    {
        var start = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return (start, end);
    }

    public async Task<SubscriptionPlan> GetPlanBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.SubscriptionPlans.AsNoTracking()
                   .FirstAsync(p => p.Slug == slug && p.IsActive, ct);
    }

    public async Task<(CompanySubscription Sub, SubscriptionPlan Plan)> EnsureCompanyAsync(Guid companyId,
        CancellationToken ct = default)
    {
        var sub = await db.CompanySubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);

        if (sub is not null)
            return (sub, sub.Plan);

        try
        {
            var now = DateTime.UtcNow;
            var (pStart, pEnd) = GetCurrentUsagePeriodUtc(now);
            var freePlan = await db.SubscriptionPlans.FirstAsync(p => p.Slug == "free", ct);

            sub = new CompanySubscription
            {
                CompanyId = companyId,
                PlanId = freePlan.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = pStart,
                CurrentPeriodEnd = pEnd
            };

            db.CompanySubscriptions.Add(sub);

            var balance = new CompanyBalance
            {
                CompanyId = companyId,
                Amount = 0,
                Currency = "USDT"
            };
            db.CompanyBalances.Add(balance);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Initialized billing for company {CompanyId}", companyId);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert — another request created the record first.
            db.ChangeTracker.Clear();
        }

        sub = await db.CompanySubscriptions
            .Include(x => x.Plan)
            .FirstAsync(x => x.CompanyId == companyId, ct);
        return (sub, sub.Plan);
    }

    public async Task<UsageRecord> GetOrCreateUsageRecordAsync(Guid companyId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var (pStart, pEnd) = GetCurrentUsagePeriodUtc(now);

        var record = await db.UsageRecords
            .FirstOrDefaultAsync(u => u.CompanyId == companyId && u.PeriodStart == pStart, ct);

        if (record is not null)
            return record;

        try
        {
            record = new UsageRecord
            {
                CompanyId = companyId,
                PeriodStart = pStart,
                PeriodEnd = pEnd
            };
            db.UsageRecords.Add(record);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert — another request created the record first.
            db.ChangeTracker.Clear();
        }

        return await db.UsageRecords
            .FirstAsync(u => u.CompanyId == companyId && u.PeriodStart == pStart, ct);
    }

    public async Task<(bool Allowed, int Used, int Limit)> CheckQuotaAsync(
        Guid companyId,
        string quotaType,
        int reportedUsage,
        CancellationToken ct = default)
    {
        var (_, plan) = await EnsureCompanyAsync(companyId, ct);
        var usage = await GetOrCreateUsageRecordAsync(companyId, ct);

        return quotaType switch
        {
            BillingPlanConstants.QuotaClients => (
                reportedUsage < plan.MaxClients,
                reportedUsage,
                plan.MaxClients),
            BillingPlanConstants.QuotaBots => (
                reportedUsage < GetLimit(plan.MaxBots),
                reportedUsage,
                GetLimit(plan.MaxBots)),
            BillingPlanConstants.QuotaTeamMembers => (
                reportedUsage < plan.MaxTeamMembers,
                reportedUsage,
                plan.MaxTeamMembers),
            BillingPlanConstants.QuotaAiExecutions => (
                usage.AiExecutionsUsed < GetLimit(plan.MaxAiExecutionsPerMonth),
                usage.AiExecutionsUsed,
                GetLimit(plan.MaxAiExecutionsPerMonth)),
            BillingPlanConstants.QuotaAiBuilder => (
                usage.AiBuilderRequestsUsed < GetLimit(plan.MaxAiBuilderRequestsPerMonth),
                usage.AiBuilderRequestsUsed,
                GetLimit(plan.MaxAiBuilderRequestsPerMonth)),
            _ => (true, 0, 0)
        };
    }

    private static int GetLimit(int value) => value >= int.MaxValue - 1 ? int.MaxValue : value;

    public async Task<bool> IncrementUsageAsync(Guid companyId, string usageType, int amount,
        CancellationToken ct = default)
    {
        await EnsureCompanyAsync(companyId, ct);
        var usage = await GetOrCreateUsageRecordAsync(companyId, ct);

        switch (usageType)
        {
            case BillingPlanConstants.QuotaAiExecutions:
                usage.AiExecutionsUsed += amount;
                break;
            case BillingPlanConstants.QuotaAiBuilder:
                usage.AiBuilderRequestsUsed += amount;
                break;
            case BillingPlanConstants.QuotaClients:
                usage.ClientsCount += amount;
                break;
            default:
                return false;
        }

        usage.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ChangePlanAsync(Guid companyId, Guid planId, CancellationToken ct = default)
    {
        var (sub, _) = await EnsureCompanyAsync(companyId, ct);
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Id == planId, ct);
        sub.PlanId = plan.Id;
        sub.Status = SubscriptionStatus.Active;
        sub.PastDueSince = null;
        sub.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CreditBalanceFromPaymentAsync(
        Guid companyId,
        decimal amount,
        string description,
        Guid? paymentId,
        CancellationToken ct = default)
    {
        await EnsureCompanyAsync(companyId, ct);
        var balance = await db.CompanyBalances.FirstAsync(b => b.CompanyId == companyId, ct);
        var before = balance.Amount;
        balance.Amount += amount;
        balance.ModifiedAt = DateTime.UtcNow;

        db.BalanceTransactions.Add(new BalanceTransaction
        {
            CompanyId = companyId,
            Type = TransactionType.TopUp,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = balance.Amount,
            Description = description,
            ReferenceId = paymentId
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryChargeMonthlyAsync(CompanySubscription sub, CancellationToken ct = default)
    {
        var plan = await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.Id == sub.PlanId, ct);
        if (plan.PricePerMonth <= 0)
            return true;

        var balance = await db.CompanyBalances.FirstOrDefaultAsync(b => b.CompanyId == sub.CompanyId, ct);
        if (balance is null)
            return false;

        if (balance.Amount < plan.PricePerMonth)
            return false;

        var before = balance.Amount;
        balance.Amount -= plan.PricePerMonth;
        balance.ModifiedAt = DateTime.UtcNow;

        db.BalanceTransactions.Add(new BalanceTransaction
        {
            CompanyId = sub.CompanyId,
            Type = TransactionType.MonthlyCharge,
            Amount = -plan.PricePerMonth,
            BalanceBefore = before,
            BalanceAfter = balance.Amount,
            Description = $"Monthly charge for plan {plan.Slug}"
        });

        var now = DateTime.UtcNow;
        sub.CurrentPeriodStart = sub.CurrentPeriodEnd;
        sub.CurrentPeriodEnd = sub.CurrentPeriodEnd.AddMonths(1);
        sub.Status = SubscriptionStatus.Active;
        sub.PastDueSince = null;
        sub.ModifiedAt = now;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
