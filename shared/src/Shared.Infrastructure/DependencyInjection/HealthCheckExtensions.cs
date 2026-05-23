using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.DependencyInjection;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddChatFoundryHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        var pgConn = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(pgConn))
        {
            healthChecks.AddNpgSql(pgConn, name: "postgresql", tags: ["db", "ready"]);
        }

        var redisHost = configuration["CacheSettings:Host"];
        var redisPort = configuration["CacheSettings:Port"] ?? "6379";
        if (!string.IsNullOrWhiteSpace(redisHost))
        {
            var redisConnectionString = configuration["CacheSettings:ConnectionString"]
                                        ?? $"{redisHost}:{redisPort}";
            healthChecks.AddRedis(redisConnectionString, name: "redis", tags: ["cache", "ready"]);
        }

        var kafkaConn = configuration.GetConnectionString("Kafka");
        if (!string.IsNullOrWhiteSpace(kafkaConn))
        {
            healthChecks.AddKafka(
                new ProducerConfig { BootstrapServers = kafkaConn },
                name: "kafka",
                tags: ["messaging", "ready"]);
        }

        return services;
    }
}
