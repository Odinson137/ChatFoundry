using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Data.Configurations;

public class AttributeDefinitionEntityConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.HasKey(ad => ad.Id);

        builder.Property(ad => ad.Key).IsRequired().HasMaxLength(50);
        builder.Property(ad => ad.DisplayName).HasMaxLength(100);
        builder.Property(ad => ad.Description).HasMaxLength(500);

        builder.Property(ad => ad.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ad => ad.ScopeEntityId).IsRequired();

        builder.HasIndex(ad => new { ad.ScopeEntityId, ad.Key }).IsUnique();
    }
}
