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
        EnsureWorkflowParameterColumns();
    }

    private void EnsureWorkflowParameterColumns()
    {
        var tableName = Model.FindEntityType(typeof(BotWorkflow))?.GetTableName();
        if (string.IsNullOrEmpty(tableName)) return;

        try
        {
#pragma warning disable EF1002 
            Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN IF NOT EXISTS \"InputParametersDefinition\" jsonb NOT NULL DEFAULT '[]'");
            Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN IF NOT EXISTS \"OutputParametersDefinition\" jsonb NOT NULL DEFAULT '[]'");
#pragma warning restore EF1002
        }
        catch
        {

        }
    }

    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<MessengerChannel> MessengerChannels => Set<MessengerChannel>();
    public DbSet<BotChannel> BotChannels => Set<BotChannel>();
    public DbSet<BotWorkflow> Workflows => Set<BotWorkflow>();
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