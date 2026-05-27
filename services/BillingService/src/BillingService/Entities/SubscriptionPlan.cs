using Shared.Domain.Entities;

namespace BillingService.Entities;

public class SubscriptionPlan : EntityBase
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public decimal PricePerMonth { get; set; }
    public int MaxClients { get; set; }
    public int MaxBots { get; set; }
    public int MaxTeamMembers { get; set; }
    public long MaxAiTokensPerMonth { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasApiAccess { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CompanySubscription> Subscriptions { get; set; } = [];
}
