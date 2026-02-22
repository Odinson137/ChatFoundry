using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class MessengerChannelConfiguration : BaseEntityTypeConfiguration<MessengerChannel>
{
    public override void Configure(EntityTypeBuilder<MessengerChannel> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ChannelType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedUserId)
            .IsRequired();
        builder.Property(x => x.CompanyId);

        builder.HasIndex(x => x.CreatedUserId);
        builder.HasIndex(x => x.CompanyId);
    }
}
