using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class WorkflowConfiguration : BaseEntityTypeConfiguration<BotWorkflow>
{
    public override void Configure(EntityTypeBuilder<BotWorkflow> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.NodesDefinition)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(x => x.EdgesDefinition)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(x => x.LayoutDefinition)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.HasIndex(x => x.BotId);

        builder.Property(x => x.Version)
            .HasDefaultValue(1);
    }
}