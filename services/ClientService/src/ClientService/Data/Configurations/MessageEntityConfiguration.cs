using ClientService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.EntityTypeConfiguration;

namespace ClientService.Data.Configurations;

public class MessageEntityConfiguration : BaseEntityTypeConfiguration<Message>
{
    public override void Configure(EntityTypeBuilder<Message> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Payload)
            .HasMaxLength(4000);

        builder.Property(x => x.Direction)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.MessageKind)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(x => x.InternalMessageId)
            .IsUnique();

        builder.HasOne(x => x.CreatedBy)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}