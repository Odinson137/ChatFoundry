using BillingService.Data;
using BillingService.Entities;
using BillingService.Enums;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Services;

public class BillingAccountService(
    BillingDbContext db,
    ILogger<BillingAccountService> logger)
{
    public static (DateTime Start, DateTime End) GetSubscriptionPeriod(CompanySubscription sub)
    {
        return (sub.CurrentPeriodStart, sub.CurrentPeriodEnd);
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
            var freePlan = await db.SubscriptionPlans.FirstAsync(p => p.Slug == "free", ct);

            sub = new CompanySubscription
            {
                CompanyId = companyId,
                PlanId = freePlan.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddMonths(1)
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

    public async Task<UsageRecord> GetOrCreateUsageRecordAsync(Guid companyId, DateTime periodStart,
        DateTime periodEnd, CancellationToken ct = default)
    {
        var record = await db.UsageRecords
            .FirstOrDefaultAsync(u => u.CompanyId == companyId && u.PeriodStart == periodStart, ct);

        if (record is not null)
            return record;

        try
        {
            record = new UsageRecord
            {
                CompanyId = companyId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };
            db.UsageRecords.Add(record);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }

        return await db.UsageRecords
            .FirstAsync(u => u.CompanyId == companyId && u.PeriodStart == periodStart, ct);
    }

    public async Task<(bool Allowed, int Used, int Limit)> CheckQuotaAsync(
        Guid companyId,
        string quotaType,
        int reportedUsage,
        CancellationToken ct = default)
    {
        var (sub, plan) = await EnsureCompanyAsync(companyId, ct);
        var (pStart, pEnd) = GetSubscriptionPeriod(sub);
        var usage = await GetOrCreateUsageRecordAsync(companyId, pStart, pEnd, ct);

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
        var (sub, _) = await EnsureCompanyAsync(companyId, ct);
        var (pStart, pEnd) = GetSubscriptionPeriod(sub);
        var usage = await GetOrCreateUsageRecordAsync(companyId, pStart, pEnd, ct);

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

    public async Task<ChangePlanResult> ChangePlanAsync(Guid companyId, Guid newPlanId,
        CancellationToken ct = default)
    {
        var (sub, oldPlan) = await EnsureCompanyAsync(companyId, ct);
        var newPlan = await db.SubscriptionPlans.FirstAsync(p => p.Id == newPlanId, ct);

        if (sub.PlanId == newPlanId)
        {
            if (sub.PendingPlanId.HasValue)
            {
                sub.PendingPlanId = null;
                sub.ModifiedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return ChangePlanResult.ClearedPending;
            }

            return ChangePlanResult.NoChange;
        }

        var isUpgrade = newPlan.PricePerMonth > oldPlan.PricePerMonth;

        if (isUpgrade)
        {
            sub.PendingPlanId = null;

            var credit = CalculateProratedAmount(oldPlan.PricePerMonth, sub);
            var charge = CalculateProratedAmount(newPlan.PricePerMonth, sub);
            var netCost = charge - credit;

            var balance = await db.CompanyBalances.FirstAsync(b => b.CompanyId == companyId, ct);

            if (balance.Amount < netCost)
                return ChangePlanResult.InsufficientBalance;

            if (credit > 0)
            {
                var creditBefore = balance.Amount;
                balance.Amount += credit;

                db.BalanceTransactions.Add(new BalanceTransaction
                {
                    CompanyId = companyId,
                    Type = TransactionType.Refund,
                    Amount = credit,
                    BalanceBefore = creditBefore,
                    BalanceAfter = balance.Amount,
                    Description =
                        $"Upgrade credit: {oldPlan.Slug} -> {newPlan.Slug} ({credit:F2} USDT for unused period)"
                });
            }

            if (netCost > 0)
            {
                var chargeBefore = balance.Amount;
                balance.Amount -= netCost;

                db.BalanceTransactions.Add(new BalanceTransaction
                {
                    CompanyId = companyId,
                    Type = TransactionType.MonthlyCharge,
                    Amount = -netCost,
                    BalanceBefore = chargeBefore,
                    BalanceAfter = balance.Amount,
                    Description =
                        $"Upgrade charge: {oldPlan.Slug} -> {newPlan.Slug} ({netCost:F2} USDT for remaining period)"
                });
            }

            balance.ModifiedAt = DateTime.UtcNow;

            sub.PlanId = newPlan.Id;
            sub.Status = SubscriptionStatus.Active;
            sub.PastDueSince = null;
            sub.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Company {CompanyId} upgraded {Old} -> {New}, credit: {Credit:F2}, charge: {Charge:F2}, net: {Net:F2}",
                companyId, oldPlan.Slug, newPlan.Slug, credit, charge, netCost);

            return ChangePlanResult.UpgradeApplied;
        }
        else
        {
            sub.PendingPlanId = newPlan.Id;
            sub.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Company {CompanyId} scheduled downgrade {Old} -> {New} at {At}",
                companyId, oldPlan.Slug, newPlan.Slug, sub.CurrentPeriodEnd);

            return ChangePlanResult.DowngradeScheduled;
        }
    }

    private static decimal CalculateProratedAmount(decimal pricePerMonth, CompanySubscription sub)
    {
        if (pricePerMonth <= 0)
            return 0;

        var now = DateTime.UtcNow;
        if (now >= sub.CurrentPeriodEnd)
            return 0;

        var totalDays = (sub.CurrentPeriodEnd - sub.CurrentPeriodStart).TotalDays;
        var remainingDays = (sub.CurrentPeriodEnd - now).TotalDays;

        if (totalDays <= 0)
            return 0;

        var amount = pricePerMonth * ((decimal)remainingDays / (decimal)totalDays);
        return Math.Floor(amount * 100) / 100;
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

        if (sub.PendingPlanId.HasValue)
        {
            var pendingPlan = await db.SubscriptionPlans.AsNoTracking()
                .FirstAsync(p => p.Id == sub.PendingPlanId.Value, ct);
            logger.LogInformation("Company {CompanyId} applying pending plan change to {Plan}",
                sub.CompanyId, pendingPlan.Slug);
            sub.PlanId = sub.PendingPlanId.Value;
            sub.PendingPlanId = null;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
