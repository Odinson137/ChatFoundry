using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Options;

namespace Shared.Infrastructure.DependencyInjection;

public static class RedisCacheExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = FoundryRedisCacheOptions.SectionName)
    {
        var section = configuration.GetSection(sectionName);
        services.Configure<FoundryRedisCacheOptions>(section);

        var options = new FoundryRedisCacheOptions();
        section.Bind(options);

        var connectionString = options.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = options.Host ?? "localhost";
            connectionString = $"{host}:{options.Port}";
        }

        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = connectionString;
            redisOptions.InstanceName = options.KeyPrefix;
        });

        return services;
    }
}
