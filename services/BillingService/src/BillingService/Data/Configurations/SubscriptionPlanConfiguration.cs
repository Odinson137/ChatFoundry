using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class SubscriptionPlanConfiguration : BaseEntityTypeConfiguration<SubscriptionPlan>
{
    public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        base.Configure(builder);
        builder.ToTable("subscription_plans");
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.PricePerMonth).HasPrecision(18, 4);
    }
}
