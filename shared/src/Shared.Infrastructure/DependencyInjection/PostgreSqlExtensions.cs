using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Options;

namespace Shared.Infrastructure.DependencyInjection;

public static class PostgreSqlExtensions
{
    public static IServiceCollection AddPostgreSql<TContext>(
        this IServiceCollection services, 
        IConfiguration configuration,
        string connectionName = "DefaultConnection")
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string '{connectionName}' is not set.");

        services.Configure<PostgreSqlOptions>(opts =>
        {
            opts.ConnectionString = connectionString;
        });

        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}