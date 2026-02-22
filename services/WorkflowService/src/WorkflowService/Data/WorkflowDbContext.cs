using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WorkflowService.Entities;

namespace WorkflowService.Data;

public class WorkflowDbContext : DbContext
{

    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<MessengerChannel> MessengerChannels => Set<MessengerChannel>();
    public DbSet<BotChannel> BotChannels => Set<BotChannel>();
    public DbSet<BotWorkflow> Workflows => Set<BotWorkflow>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<ActionEntity> Actions => Set<ActionEntity>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Session>()
            .Property(s => s.Variables)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>(),
                new ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => c.ToDictionary(entry => entry.Key, entry => entry.Value)));
        
        modelBuilder.Entity<Session>()
            .Ignore(s => s.ClientProfileDirty);
    }
}