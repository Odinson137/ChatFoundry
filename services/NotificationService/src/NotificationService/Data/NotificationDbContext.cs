using Microsoft.EntityFrameworkCore;
using NotificationService.Entities;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public DbSet<LiveChatSession> LiveChatSessions => Set<LiveChatSession>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
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
