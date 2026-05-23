using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Shared.Infrastructure.DependencyInjection;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddChatFoundrySerilog(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var seqUrl = builder.Configuration["Observability:Seq:Url"] ?? "http://seq:5341";
        var seqApiKey = builder.Configuration["Observability:Seq:ApiKey"] ?? string.Empty;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.Seq(seqUrl, apiKey: string.IsNullOrEmpty(seqApiKey) ? null : seqApiKey)
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}
