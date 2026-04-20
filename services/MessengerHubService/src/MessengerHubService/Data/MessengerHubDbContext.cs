using Microsoft.EntityFrameworkCore;
using MessengerHubService.Entities;

namespace MessengerHubService.Data;

public class MessengerHubDbContext : DbContext
{
    public DbSet<LiveChatSession> LiveChatSessions => Set<LiveChatSession>();

    public MessengerHubDbContext(DbContextOptions<MessengerHubDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LiveChatSession>()
            .HasIndex(s => s.Status);
        modelBuilder.Entity<LiveChatSession>()
            .HasIndex(s => s.CompanyId);
        modelBuilder.Entity<LiveChatSession>()
            .HasIndex(s => new { s.ChannelId, s.ExternalUserId, s.Status });
    }
}
