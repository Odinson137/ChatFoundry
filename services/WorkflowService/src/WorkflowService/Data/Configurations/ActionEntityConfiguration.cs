using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class ActionEntityConfiguration : BaseEntityTypeConfiguration<ActionEntity>
{
    public override void Configure(EntityTypeBuilder<ActionEntity> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.NodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Payload)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.MessageKind)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.SessionId);

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Actions)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}