using BillingService.Data;
using BillingService.Enums;
using BillingService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;
using MassTransit;
using Shared.Application.Events;

namespace BillingService.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class BillingMutation(
    IHttpContextAccessor httpContextAccessor,
    IGraphQlCacheService cacheService,
    ITopicProducer<CompanySubscriptionChangedEvent> producer) : BaseGraphQl(httpContextAccessor)
{
    public async Task<ChangePlanResultDto> ChangeSubscriptionPlan(
        string planSlug,
        [Service] BillingDbContext db,
        [Service] BillingAccountService account,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("company required");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Slug == planSlug && p.IsActive, ct)
                   ?? throw new GraphQLException("Unknown plan");

        var result = await account.ChangePlanAsync(CompanyId.Value, plan.Id, ct);

        if (result == ChangePlanResult.InsufficientBalance)
            throw new GraphQLException("Insufficient balance for this plan change");

        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:billing-overview" }, ct);

        if (result != ChangePlanResult.NoChange)
        {
            try
            {
                await producer.Produce(new CompanySubscriptionChangedEvent(CompanyId.Value), ct);
            }
            catch
            {
            }
        }

        DateTime? pendingAt = null;
        decimal? credit = null;

        if (result == ChangePlanResult.DowngradeScheduled)
        {
            var sub = await db.CompanySubscriptions.AsNoTracking()
                .FirstAsync(x => x.CompanyId == CompanyId.Value, ct);
            pendingAt = sub.CurrentPeriodEnd;
        }

        return new ChangePlanResultDto(
            result != ChangePlanResult.NoChange,
            result.ToString(),
            result == ChangePlanResult.DowngradeScheduled ? planSlug : null,
            pendingAt,
            credit);
    }

    public async Task<TopUpPayload> CreateTopUpInvoice(
        decimal amount,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("company required");

        return new TopUpPayload(false, null, "Online top-up is disabled. Please contact the administrator to manually add funds.");
    }
}
