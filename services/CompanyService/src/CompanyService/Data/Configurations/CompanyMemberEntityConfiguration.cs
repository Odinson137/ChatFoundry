using CompanyService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Data.Configurations;

public class CompanyMemberEntityConfiguration : IEntityTypeConfiguration<CompanyMember>
{
    public void Configure(EntityTypeBuilder<CompanyMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.CompanyId, m.UserId })
            .IsUnique();

        builder.Property(m => m.Role)
            .HasConversion<int>();

        builder.HasIndex(m => m.UserId);
    }
}
