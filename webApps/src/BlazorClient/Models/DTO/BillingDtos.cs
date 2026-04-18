namespace BlazorClient.Models.DTO;

public class BillingOverviewDto
{
    public string PlanSlug { get; set; } = "";
    public string SubscriptionStatus { get; set; } = "";
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USDT";
    public DateTime CurrentPeriodEnd { get; set; }
    public int MaxClients { get; set; }
    public int MaxBots { get; set; }
    public int MaxTeamMembers { get; set; }
    public int AiExecutionsUsed { get; set; }
    public int MaxAiExecutions { get; set; }
    public int AiBuilderUsed { get; set; }
    public int MaxAiBuilder { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasApiAccess { get; set; }
    public string? PendingPlanSlug { get; set; }
    public DateTime? PendingChangeAt { get; set; }
}

public class BalanceTransactionDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
}

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public decimal PricePerMonth { get; set; }
    public int MaxClients { get; set; }
    public int MaxBots { get; set; }
    public int MaxTeamMembers { get; set; }
    public int MaxAiExecutions { get; set; }
    public int MaxAiBuilder { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasApiAccess { get; set; }
}

public class TopUpResultDto
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Error { get; set; }
}

public class ChangePlanResultDto
{
    public bool Success { get; set; }
    public string ResultType { get; set; } = "";
    public string? PendingPlanSlug { get; set; }
    public DateTime? PendingChangeAt { get; set; }
    public decimal? CreditApplied { get; set; }
}
