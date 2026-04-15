using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IBillingApiClient
{
    Task<BillingOverviewDto?> GetBillingOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BalanceTransactionDto>> GetBalanceTransactionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlanDto>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default);
    Task<bool> ChangeSubscriptionPlanAsync(string planSlug, CancellationToken cancellationToken = default);
    Task<TopUpResultDto> CreateTopUpInvoiceAsync(decimal amount, CancellationToken cancellationToken = default);
}
