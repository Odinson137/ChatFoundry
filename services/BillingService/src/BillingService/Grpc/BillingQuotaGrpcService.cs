using Billing.Grpc;
using BillingService.Data;
using BillingService.Services;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Grpc;

public class BillingQuotaGrpcService(
    BillingAccountService account,
    BillingDbContext db) : global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceBase
{
    public override async Task<CheckQuotaResponse> CheckQuota(CheckQuotaRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CompanyId, out var companyId))
            return new CheckQuotaResponse { Allowed = false, Used = 0, Limit = 0 };

        var (allowed, used, limit) = await account.CheckQuotaAsync(
            companyId,
            request.QuotaType,
            request.ReportedUsage,
            context.CancellationToken);

        return new CheckQuotaResponse
        {
            Allowed = allowed,
            Used = used,
            Limit = limit
        };
    }

    public override async Task<GetCompanyPlanResponse> GetCompanyPlan(GetCompanyPlanRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.CompanyId, out var companyId))
            return new GetCompanyPlanResponse();

        var (sub, _) = await account.EnsureCompanyAsync(companyId, context.CancellationToken);
        var plan = await db.SubscriptionPlans.AsNoTracking()
            .FirstAsync(p => p.Id == sub.PlanId, context.CancellationToken);

        return new GetCompanyPlanResponse
        {
            PlanSlug = plan.Slug,
            Status = sub.Status.ToString(),
            MaxClients = plan.MaxClients,
            MaxBots = plan.MaxBots,
            MaxTeamMembers = plan.MaxTeamMembers,
            MaxAiExecutions = plan.MaxAiExecutionsPerMonth,
            MaxAiBuilderRequests = plan.MaxAiBuilderRequestsPerMonth,
            HasAnalytics = plan.HasAnalytics,
            HasApiAccess = plan.HasApiAccess
        };
    }

    public override async Task<IncrementUsageResponse> IncrementUsage(IncrementUsageRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.CompanyId, out var companyId))
            return new IncrementUsageResponse { Success = false };

        var ok = await account.IncrementUsageAsync(
            companyId,
            request.UsageType,
            request.Amount,
            context.CancellationToken);
        return new IncrementUsageResponse { Success = ok };
    }
}
