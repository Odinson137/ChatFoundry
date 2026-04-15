using BillingService.Data;
using BillingService.Services;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace BillingService.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class BillingMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<bool> ChangeSubscriptionPlan(
        string planSlug,
        [Service] BillingDbContext db,
        [Service] BillingAccountService account,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("company required");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Slug == planSlug && p.IsActive, ct)
                   ?? throw new GraphQLException("Unknown plan");

        await account.ChangePlanAsync(CompanyId.Value, plan.Id, ct);
        return true;
    }

    public async Task<TopUpPayload> CreateTopUpInvoice(
        decimal amount,
        [Service] HeleketPaymentService heleket,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("company required");
        if (amount <= 0)
            return new TopUpPayload(false, null, "Amount must be positive");

        var result = await heleket.CreateTopUpInvoiceAsync(CompanyId.Value, amount, ct);
        if (result is null)
            return new TopUpPayload(false, null, "Payment provider unavailable");

        return new TopUpPayload(true, result.PaymentUrl, null);
    }
}
