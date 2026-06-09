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
                MaxBots = 10,
                MaxTeamMembers = 10,
                MaxAiTokensPerMonth = 1_000_000,
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
                MaxClients = 2_000,
                MaxBots = 25,
                MaxTeamMembers = 25,
                MaxAiTokensPerMonth = 2_000_000,
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
                MaxClients = 20_000,
                MaxBots = 50,
                MaxTeamMembers = 50,
                MaxAiTokensPerMonth = 4_000_000,
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
                MaxClients = 100_000,
                MaxBots = max,
                MaxTeamMembers = 100,
                MaxAiTokensPerMonth = 20_000_000,
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
