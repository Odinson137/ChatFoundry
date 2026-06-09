using BillingService.Data;
using BillingService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace BillingService.GraphQL;

[ExtendObjectType(typeof(Query))]
public class BillingQuery(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<BillingOverviewDto?> BillingOverview(
        [Service] BillingDbContext db,
        [Service] BillingAccountService account,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            return null;

        var sub = await db.CompanySubscriptions
            .Include(x => x.Plan)
            .Include(x => x.PendingPlan)
            .FirstOrDefaultAsync(x => x.CompanyId == CompanyId.Value, ct);

        if (sub is null)
            (sub, _) = await account.EnsureCompanyAsync(CompanyId.Value, ct);

        var plan = await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.Id == sub.PlanId, ct);
        var balance = await db.CompanyBalances.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyId == CompanyId.Value, ct);

        var (pStart, pEnd) = BillingAccountService.GetSubscriptionPeriod(sub);
        var usage = await account.GetOrCreateUsageRecordAsync(CompanyId.Value, pStart, pEnd, ct);

        string? pendingPlanSlug = null;
        DateTime? pendingChangeAt = null;
        if (sub.PendingPlanId.HasValue)
        {
            pendingPlanSlug = sub.PendingPlan?.Slug
                ?? await db.SubscriptionPlans.AsNoTracking()
                    .Where(p => p.Id == sub.PendingPlanId.Value)
                    .Select(p => p.Slug)
                    .FirstOrDefaultAsync(ct);
            pendingChangeAt = sub.CurrentPeriodEnd;
        }

        static int Cap(int v) => v >= int.MaxValue - 1 ? int.MaxValue : v;
        static long CapLong(long v) => v >= long.MaxValue - 1 ? long.MaxValue : v;

        return new BillingOverviewDto(
            plan.Slug,
            sub.Status.ToString(),
            balance?.Amount ?? 0,
            balance?.Currency ?? "USD",
            sub.CurrentPeriodEnd,
            plan.MaxClients,
            Cap(plan.MaxBots),
            plan.MaxTeamMembers,
            usage.AiTokensUsed,
            CapLong(plan.MaxAiTokensPerMonth),
            plan.HasAnalytics,
            plan.HasApiAccess,
            pendingPlanSlug,
            pendingChangeAt);
    }

    public async Task<IReadOnlyList<BalanceTransactionDto>> BalanceTransactions(
        [Service] BillingDbContext db,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            return [];

        return await db.BalanceTransactions.AsNoTracking()
            .Where(t => t.CompanyId == CompanyId.Value)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .Select(t => new BalanceTransactionDto(
                t.Id,
                t.CreatedAt,
                t.Type.ToString(),
                t.Amount,
                t.BalanceAfter,
                t.Description))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> SubscriptionPlans(
        [Service] BillingDbContext db,
        CancellationToken ct)
    {
        static int Cap(int v) => v >= int.MaxValue - 1 ? int.MaxValue : v;

        static long CapLong(long v) => v >= long.MaxValue - 1 ? long.MaxValue : v;

        var rows = await db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return rows.Select(p => new SubscriptionPlanDto(
                p.Id,
                p.Name,
                p.Slug,
                p.PricePerMonth,
                p.MaxClients,
                Cap(p.MaxBots),
                p.MaxTeamMembers,
                CapLong(p.MaxAiTokensPerMonth),
                p.HasAnalytics,
                p.HasApiAccess))
            .ToList();
    }
}
