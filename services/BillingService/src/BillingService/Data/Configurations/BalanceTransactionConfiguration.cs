using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class BalanceTransactionConfiguration : BaseEntityTypeConfiguration<BalanceTransaction>
{
    public override void Configure(EntityTypeBuilder<BalanceTransaction> builder)
    {
        base.Configure(builder);
        builder.ToTable("balance_transactions");
        builder.HasIndex(x => x.CompanyId);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.BalanceBefore).HasPrecision(18, 4);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        builder.Property(x => x.Description).HasMaxLength(512);
    }
}
