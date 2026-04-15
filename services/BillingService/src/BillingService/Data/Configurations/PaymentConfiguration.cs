using BillingService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace BillingService.Data.Configurations;

public class PaymentConfiguration : BaseEntityTypeConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);
        builder.ToTable("payments");
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.Property(x => x.HeleketUuid).HasMaxLength(128);
        builder.Property(x => x.OrderId).HasMaxLength(128);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.AmountUsd).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(16);
        builder.Property(x => x.Network).HasMaxLength(32);
    }
}
