using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class CompanySubscriptionConfiguration : BaseEntityTypeConfiguration<CompanySubscription>
{
    public override void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        base.Configure(builder);
        builder.ToTable("company_subscriptions");
        builder.HasIndex(x => x.CompanyId).IsUnique();
        builder.HasOne(x => x.Plan)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.PlanId);
    }
}
