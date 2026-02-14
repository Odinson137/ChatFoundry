using System.Reflection;
using FileService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileService.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
    {
    }

    public DbSet<FileEntity> Files => Set<FileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
