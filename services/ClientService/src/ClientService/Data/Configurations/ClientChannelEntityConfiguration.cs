using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace ClientService.Data.Configurations;

public class ClientChannelEntityConfiguration : BaseEntityTypeConfiguration<ClientChannel>
{
    public override void Configure(EntityTypeBuilder<ClientChannel> builder)
    {
        base.Configure(builder);
        
        builder.Property(x => x.Channel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ExternalUserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Username)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.Channel, x.ExternalUserId })
            .IsUnique();

        builder.HasIndex(x => x.Phone);
        builder.HasIndex(x => x.Email);
        
        builder.HasMany(x => x.Messages)
            .WithOne(x => x.ClientChannel)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}