using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Shared.Infrastructure.DependencyInjection;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddChatFoundryObservability(
        this WebApplicationBuilder builder,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        builder.AddChatFoundrySerilog(serviceName);

        builder.Services.AddChatFoundryTelemetry(
            builder.Configuration, serviceName, serviceVersion);

        builder.Services.AddChatFoundryHealthChecks(builder.Configuration);

        return builder;
    }

    public static WebApplication UseChatFoundryObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
            {
                diagCtx.Set("RequestHost", httpCtx.Request.Host.Value ?? "");
                diagCtx.Set("RequestScheme", httpCtx.Request.Scheme);
                var userId = httpCtx.User?.FindFirst("sub")?.Value;
                if (userId is not null)
                    diagCtx.Set("UserId", userId);
            };
        });

        app.MapHealthChecks("/health");

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        app.MapPrometheusScrapingEndpoint();

        return app;
    }
}
