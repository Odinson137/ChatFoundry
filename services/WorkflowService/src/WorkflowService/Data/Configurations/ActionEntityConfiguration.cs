using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class ActionEntityConfiguration : IEntityTypeConfiguration<ActionEntity>
{
    public void Configure(EntityTypeBuilder<ActionEntity> builder)
    {
        builder.ToTable("actions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Payload)
            .HasMaxLength(1000);
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(x => x.SessionId);

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Actions)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}