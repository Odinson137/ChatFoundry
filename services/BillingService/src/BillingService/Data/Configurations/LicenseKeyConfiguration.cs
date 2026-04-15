using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class LicenseKeyConfiguration : BaseEntityTypeConfiguration<LicenseKey>
{
    public override void Configure(EntityTypeBuilder<LicenseKey> builder)
    {
        base.Configure(builder);
        builder.ToTable("license_keys");
        builder.Property(x => x.KeyHash).HasMaxLength(128);
        builder.Property(x => x.Tier).HasMaxLength(64);
    }
}
