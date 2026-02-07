using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace ClientService.Data.Configurations;

public class ClientAttributeEntityConfiguration : BaseEntityTypeConfiguration<ClientAttribute>
{
    public override void Configure(EntityTypeBuilder<ClientAttribute> builder)
    {
        base.Configure(builder);

        builder.Property(a => a.Key).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Value).IsRequired().HasMaxLength(4000);

        builder.HasOne(a => a.ClientChannel)
            .WithMany(c => c.Attributes)
            .HasForeignKey(a => a.ClientChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ClientChannelId, a.Key }).IsUnique();
    }
}
