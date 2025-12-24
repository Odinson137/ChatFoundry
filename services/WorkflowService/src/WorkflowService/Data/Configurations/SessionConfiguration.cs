using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CurrentNodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>(); // или int

        builder.Property(x => x.CompletedAt);

        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => new { x.ClientId, x.Channel });

        builder.HasOne(x => x.Workflow)
            .WithMany()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}