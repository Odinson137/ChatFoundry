using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("workflows");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SchemaJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.BotId);

        builder.Property(x => x.Version)
            .HasDefaultValue(1);
    }
}