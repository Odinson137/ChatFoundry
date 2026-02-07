using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Data.Configurations;

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.HasMany(t => t.Clients)
            .WithOne(c => c.Team)
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.AttributeDefinitions)
            .WithOne(ad => ad.Team)
            .HasForeignKey(ad => ad.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
