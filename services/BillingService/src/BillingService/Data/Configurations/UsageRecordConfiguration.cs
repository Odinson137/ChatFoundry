using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class UsageRecordConfiguration : BaseEntityTypeConfiguration<UsageRecord>
{
    public override void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        base.Configure(builder);
        builder.ToTable("usage_records");
        builder.HasIndex(x => new { x.CompanyId, x.PeriodStart }).IsUnique();
    }
}
