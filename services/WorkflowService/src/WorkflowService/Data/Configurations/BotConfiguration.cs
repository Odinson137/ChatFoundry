using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;


public class BotConfiguration : BaseEntityTypeConfiguration<Bot>
{
    public override void Configure(EntityTypeBuilder<Bot> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CreatedUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.CreatedUserId);
        builder.HasIndex(x => x.CompanyId);
        
        builder.HasMany(b => b.Workflows)
            .WithOne(w => w.Bot)
            .HasForeignKey(w => w.BotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.BotChannels)
            .WithOne(bc => bc.Bot)
            .HasForeignKey(bc => bc.BotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}