using Microsoft.EntityFrameworkCore;
using WorkflowService.Entities;

namespace WorkflowService.Data;

public class WorkflowDbContext : DbContext
{

    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<ActionEntity> Actions => Set<ActionEntity>();
    public DbSet<Session> Sessions => Set<Session>();
}