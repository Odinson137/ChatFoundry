using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class CompanyBalanceConfiguration : BaseEntityTypeConfiguration<CompanyBalance>
{
    public override void Configure(EntityTypeBuilder<CompanyBalance> builder)
    {
        base.Configure(builder);
        builder.ToTable("company_balances");
        builder.HasIndex(x => x.CompanyId).IsUnique();
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(16);
    }
}
