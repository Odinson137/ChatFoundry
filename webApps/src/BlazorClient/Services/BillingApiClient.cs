using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class BillingApiClient(HttpClient http) : IBillingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<BillingOverviewDto?> GetBillingOverviewAsync(CancellationToken cancellationToken = default)
    {
        const string query = """
            query {
              billingOverview {
                planSlug
                subscriptionStatus
                balance
                currency
                currentPeriodEnd
                maxClients
                maxBots
                maxTeamMembers
                aiTokensUsed
                maxAiTokens
                hasAnalytics
                hasApiAccess
                pendingPlanSlug
                pendingChangeAt
              }
            }
            """;

        var data = await ExecuteGraphQl<BillingOverviewData>(query, null, cancellationToken);
        return data.BillingOverview;
    }

    public async Task<IReadOnlyList<BalanceTransactionDto>> GetBalanceTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string query = """
            query {
              balanceTransactions {
                id
                createdAt
                type
                amount
                balanceAfter
                description
              }
            }
            """;

        var data = await ExecuteGraphQl<BalanceTransactionsData>(query, null, cancellationToken);
        return data.BalanceTransactions ?? [];
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
    {
        const string query = """
            query {
              subscriptionPlans {
                id
                name
                slug
                pricePerMonth
                maxClients
                maxBots
                maxTeamMembers
                maxAiTokens
                hasAnalytics
                hasApiAccess
              }
            }
            """;

        var data = await ExecuteGraphQl<SubscriptionPlansData>(query, null, cancellationToken);
        return data.SubscriptionPlans ?? [];
    }

    public async Task<ChangePlanResultDto?> ChangeSubscriptionPlanAsync(string planSlug,
        CancellationToken cancellationToken = default)
    {
        var query = """
            mutation($slug: String!) {
              changeSubscriptionPlan(planSlug: $slug) {
                success
                resultType
                pendingPlanSlug
                pendingChangeAt
                creditApplied
              }
            }
            """;
        var variables = new { slug = planSlug };
        var data = await ExecuteGraphQl<ChangePlanMutationData>(query, variables, cancellationToken);
        return data.ChangeSubscriptionPlan;
    }

    public async Task<TopUpResultDto> CreateTopUpInvoiceAsync(decimal amount,
        CancellationToken cancellationToken = default)
    {
        var query = """
            mutation($amount: Decimal!) {
              createTopUpInvoice(amount: $amount) {
                success
                paymentUrl
                error
              }
            }
            """;
        var variables = new { amount };
        var data = await ExecuteGraphQl<TopUpMutationData>(query, variables, cancellationToken);
        return data.CreateTopUpInvoice ?? new TopUpResultDto { Success = false, Error = "Empty response" };
    }

    private async Task<T> ExecuteGraphQl<T>(string query, object? variables,
        CancellationToken cancellationToken)
    {
        return await http.PostGraphQlAsync<T>($"{ApiEndpoints.Api}/billing/graphql", query, variables, cancellationToken);
    }

    private sealed class BillingOverviewData
    {
        public BillingOverviewDto? BillingOverview { get; set; }
    }

    private sealed class BalanceTransactionsData
    {
        public List<BalanceTransactionDto>? BalanceTransactions { get; set; }
    }

    private sealed class SubscriptionPlansData
    {
        public List<SubscriptionPlanDto>? SubscriptionPlans { get; set; }
    }

    private sealed class ChangePlanMutationData
    {
        public ChangePlanResultDto? ChangeSubscriptionPlan { get; set; }
    }

    private sealed class TopUpMutationData
    {
        public TopUpResultDto? CreateTopUpInvoice { get; set; }
    }
}
