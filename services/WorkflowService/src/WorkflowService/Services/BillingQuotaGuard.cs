using Billing.Grpc;
using Grpc.Core;

namespace WorkflowService.Services;

public class BillingQuotaGuard(
    global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient client,
    IConfiguration configuration,
    ILogger<BillingQuotaGuard> logger)
{
    private bool Enabled => configuration.GetValue("Billing:Enabled", true);

    public async Task EnsureQuotaAsync(Guid? companyId, string quotaType, int reportedUsage,
        CancellationToken ct = default)
    {
        if (!Enabled || companyId is null)
            return;

        try
        {
            var r = await client.CheckQuotaAsync(new CheckQuotaRequest
            {
                CompanyId = companyId.Value.ToString("D"),
                QuotaType = quotaType,
                ReportedUsage = reportedUsage
            }, cancellationToken: ct);

            if (!r.Allowed)
                throw new InvalidOperationException(
                    $"Quota exceeded ({quotaType}). Limit: {r.Limit}, used: {r.Used}.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            logger.LogWarning(ex, "Billing unavailable; allowing {QuotaType}", quotaType);
        }
    }

    public async Task IncrementUsageAsync(Guid? companyId, string usageType, int amount,
        CancellationToken ct = default)
    {
        if (!Enabled || companyId is null)
            return;

        try
        {
            await client.IncrementUsageAsync(new IncrementUsageRequest
            {
                CompanyId = companyId.Value.ToString("D"),
                UsageType = usageType,
                Amount = amount
            }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            logger.LogWarning(ex, "Billing unavailable; skipping usage increment");
        }
    }
}
