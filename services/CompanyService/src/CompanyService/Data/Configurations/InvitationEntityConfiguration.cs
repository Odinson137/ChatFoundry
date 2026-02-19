using CompanyService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace CompanyService.Data.Configurations;

public class InvitationEntityConfiguration : BaseEntityTypeConfiguration<Invitation>
{
    public override void Configure(EntityTypeBuilder<Invitation> builder)
    {
        base.Configure(builder);

        builder.HasIndex(i => i.CompanyId);
        builder.Property(i => i.Role).HasConversion<int>();
    }
}