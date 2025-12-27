using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class WorkflowConfiguration : BaseEntityTypeConfiguration<Workflow>
{
    public override void Configure(EntityTypeBuilder<Workflow> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SchemaJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.BotId);

        builder.Property(x => x.Version)
            .HasDefaultValue(1);
    }
}