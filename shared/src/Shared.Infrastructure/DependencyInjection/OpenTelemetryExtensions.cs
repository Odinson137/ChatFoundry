using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Infrastructure.DependencyInjection;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddChatFoundryTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        var jaegerEndpoint = configuration["Observability:Jaeger:Endpoint"] ?? "http://jaeger:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddSource("MassTransit")
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.Filter = (_, cmd) =>
                        !cmd.CommandText.Contains("__EFMigrationsHistory");
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(jaegerEndpoint);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit")
                .AddPrometheusExporter());

        return services;
    }
}
