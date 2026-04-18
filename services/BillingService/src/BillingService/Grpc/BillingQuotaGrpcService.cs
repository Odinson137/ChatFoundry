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

        var sub = await db.CompanySubscriptions
            .Include(x => x.Plan)
            .Include(x => x.PendingPlan)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, context.CancellationToken);

        if (sub is null)
            (sub, _) = await account.EnsureCompanyAsync(companyId, context.CancellationToken);

        var plan = sub.Plan;

        var response = new GetCompanyPlanResponse
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

        if (sub.PendingPlanId.HasValue)
        {
            response.PendingPlanSlug = sub.PendingPlan?.Slug ?? "";
            response.PendingChangeAt = sub.CurrentPeriodEnd.ToString("o");
        }

        return response;
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
