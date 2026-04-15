using BillingService.Enums;
using Shared.Domain.Entities;

namespace BillingService.Entities;

public class CompanySubscription : EntityBase
{
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? PastDueSince { get; set; }
}
