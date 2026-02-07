using System.Reflection;
using ClientService.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Data;

public class ClientDbContext : DbContext
{

    public ClientDbContext(DbContextOptions<ClientDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientChannel> ClientChannels => Set<ClientChannel>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<ClientAttribute> ClientAttributes => Set<ClientAttribute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}