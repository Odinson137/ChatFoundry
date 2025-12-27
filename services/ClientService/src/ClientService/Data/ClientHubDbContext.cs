using ClientService.Entities;
using Microsoft.EntityFrameworkCore;

namespace WorkflowService.Data;

public class ClientDbContext : DbContext
{

    public ClientDbContext(DbContextOptions<ClientDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientChannel> ClientChannels => Set<ClientChannel>();
    public DbSet<Message> Messages => Set<Message>();
}