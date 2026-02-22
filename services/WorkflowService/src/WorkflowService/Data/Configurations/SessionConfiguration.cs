using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class SessionConfiguration : BaseEntityTypeConfiguration<Session>
{
    public override void Configure(EntityTypeBuilder<Session> builder)
    {
        base.Configure(builder);
        
        builder.Property(x => x.ClientId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasConversion<int>(); 

        builder.Property(x => x.CurrentNodeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>(); 

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.ChannelId)
            .IsRequired();

        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => new { x.ClientId, x.Channel });
        builder.HasIndex(x => new { x.ChannelId, x.ClientId, x.WorkflowId });

        builder.HasOne(x => x.Workflow)
            .WithMany()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MessengerChannel)
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}