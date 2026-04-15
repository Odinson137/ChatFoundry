using BillingService.Data;
using BillingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Services;

public static class BillingPlanSeedService
{
    public static async Task SeedPlansAsync(BillingDbContext db, CancellationToken ct = default)
    {
        if (await db.SubscriptionPlans.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var max = int.MaxValue;

        var plans = new[]
        {
            new SubscriptionPlan
            {
                Id = BillingPlanConstants.FreePlanId,
                Name = "Free",
                Slug = "free",
                PricePerMonth = 0,
                MaxClients = 100,
                MaxBots = 3,
                MaxTeamMembers = 3,
                MaxAiExecutionsPerMonth = 100,
                MaxAiBuilderRequestsPerMonth = 10,
                HasAnalytics = false,
                HasApiAccess = false,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new SubscriptionPlan
            {
                Id = BillingPlanConstants.StarterPlanId,
                Name = "Starter",
                Slug = "starter",
                PricePerMonth = 9,
                MaxClients = 1_000,
                MaxBots = 5,
                MaxTeamMembers = 5,
                MaxAiExecutionsPerMonth = 500,
                MaxAiBuilderRequestsPerMonth = 50,
                HasAnalytics = false,
                HasApiAccess = false,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new SubscriptionPlan
            {
                Id = BillingPlanConstants.ProPlanId,
                Name = "Pro",
                Slug = "pro",
                PricePerMonth = 29,
                MaxClients = 10_000,
                MaxBots = 15,
                MaxTeamMembers = 10,
                MaxAiExecutionsPerMonth = 3_000,
                MaxAiBuilderRequestsPerMonth = 200,
                HasAnalytics = true,
                HasApiAccess = false,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new SubscriptionPlan
            {
                Id = BillingPlanConstants.BusinessPlanId,
                Name = "Business",
                Slug = "business",
                PricePerMonth = 79,
                MaxClients = 50_000,
                MaxBots = max,
                MaxTeamMembers = 25,
                MaxAiExecutionsPerMonth = 15_000,
                MaxAiBuilderRequestsPerMonth = max,
                HasAnalytics = true,
                HasApiAccess = true,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now
            }
        };

        db.SubscriptionPlans.AddRange(plans);
        await db.SaveChangesAsync(ct);
    }
}
