using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;


public class BotConfiguration : IEntityTypeConfiguration<Bot>
{
    public void Configure(EntityTypeBuilder<Bot> builder)
    {
        builder.ToTable("bots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(b => b.Workflows)
            .WithOne(w => w.Bot)
            .HasForeignKey(w => w.BotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}