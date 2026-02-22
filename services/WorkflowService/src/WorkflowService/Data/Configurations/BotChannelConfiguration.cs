using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowService.Entities;

namespace WorkflowService.Data.Configurations;

public class BotChannelConfiguration : IEntityTypeConfiguration<BotChannel>
{
    public void Configure(EntityTypeBuilder<BotChannel> builder)
    {
        builder.HasKey(x => new { x.BotId, x.ChannelId });

        builder.HasOne(x => x.Bot)
            .WithMany(b => b.BotChannels)
            .HasForeignKey(x => x.BotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Channel)
            .WithMany(c => c.BotChannels)
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChannelId);
    }
}
