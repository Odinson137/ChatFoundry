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
        
        builder.HasOne(ad => ad.Team)
            .WithMany(t => t.AttributeDefinitions)
            .HasForeignKey(ad => ad.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(ad => new { ad.TeamId, ad.Key }).IsUnique();
    }
}
