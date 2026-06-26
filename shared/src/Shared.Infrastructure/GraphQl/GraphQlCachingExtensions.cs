using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Shared.Infrastructure.Options;

namespace Shared.Infrastructure.GraphQl;

public static class GraphQlCachingExtensions
{
    public static IServiceCollection AddGraphQlCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрация опций GraphQlCache
        services.Configure<GraphQlCacheOptions>(configuration.GetSection(GraphQlCacheOptions.SectionName));

        // Регистрация IConnectionMultiplexer (используем параметры из FoundryRedisCacheOptions)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FoundryRedisCacheOptions>>().Value;
            var connectionString = options.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var host = options.Host ?? "localhost";
                connectionString = $"{host}:{options.Port}";
            }
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Регистрация сервиса кэша
        services.AddScoped<IGraphQlCacheService, GraphQlCacheService>();

        return services;
    }

    public static IApplicationBuilder UseGraphQlCaching(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GraphQlResultCacheMiddleware>();
    }
}
