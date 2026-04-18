namespace BillingService.GraphQL;

public record BillingOverviewDto(
    string PlanSlug,
    string SubscriptionStatus,
    decimal Balance,
    string Currency,
    DateTime CurrentPeriodEnd,
    int MaxClients,
    int MaxBots,
    int MaxTeamMembers,
    int AiExecutionsUsed,
    int MaxAiExecutions,
    int AiBuilderUsed,
    int MaxAiBuilder,
    bool HasAnalytics,
    bool HasApiAccess,
    string? PendingPlanSlug,
    DateTime? PendingChangeAt);

public record BalanceTransactionDto(
    Guid Id,
    DateTime CreatedAt,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description);

public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string Slug,
    decimal PricePerMonth,
    int MaxClients,
    int MaxBots,
    int MaxTeamMembers,
    int MaxAiExecutions,
    int MaxAiBuilder,
    bool HasAnalytics,
    bool HasApiAccess);

public record TopUpPayload(bool Success, string? PaymentUrl, string? Error);

public record ChangePlanResultDto(
    bool Success,
    string ResultType,
    string? PendingPlanSlug,
    DateTime? PendingChangeAt,
    decimal? CreditApplied);
