using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace ClientService.Data.Configurations;

public class ClientEntityConfiguration : BaseEntityTypeConfiguration<Client>
{
    public override void Configure(EntityTypeBuilder<Client> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.CompanyId);
        builder.Property(x => x.DisplayName)
            .HasMaxLength(200);

        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.ClientChannels)
            .WithOne(x => x.Client)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}