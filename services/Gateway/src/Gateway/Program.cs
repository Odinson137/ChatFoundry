using System.Net.Http.Headers;
using Gateway;
using OpenIddict.Validation.AspNetCore;
using Shared.Infrastructure.DependencyInjection;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("gateway");

builder.Configuration.AddEnvironmentVariables(prefix: null);

var secretToken = builder.Configuration["Telegram:SecretToken"]
    ?? throw new InvalidOperationException(
        "Переменная окружения Telegram__SecretToken не найдена. " +
        "Убедитесь, что .env подключён в docker-compose для gateway-service.");

builder.Configuration["ReverseProxy:Routes:telegram-hook-route:Match:Headers:0:Values:0"] = secretToken;

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "https://localhost:7555", "http://localhost:7555" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "BlazorClientPolicy",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var encryptionKeyBase64 = builder.Configuration["OpenIddict:EncryptionKey"];
if (string.IsNullOrEmpty(encryptionKeyBase64))
    throw new InvalidOperationException(
        "OpenIddict:EncryptionKey не задан. Задайте переменную окружения OpenIddict__EncryptionKey (или в appsettings). ");

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        var issuerUrl = builder.Configuration["IdentityService:JwtIssuer"] ?? "http://identity-service:8080/";
        options.SetIssuer(new Uri(issuerUrl));
        options.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Convert.FromBase64String(encryptionKeyBase64)));
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("workflow", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("workflow");
        });
    });

    options.AddPolicy("client", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("client");
        });
    });

    options.AddPolicy("telegram", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("telegram");
        });
    });

    options.AddPolicy("identity", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("identity");
        });
    });

    options.AddPolicy("company", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("company");
        });
    });

    options.AddPolicy("file", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("file");
        });
    });

    options.AddPolicy("billing", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("billing");
        });
    });

    options.AddPolicy("notification", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireAssertion(ctx =>
        {
            var scopes = ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains("notification");
        });
    });

});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        var policy = transformBuilderContext.Route.AuthorizationPolicy;
        if (string.IsNullOrEmpty(policy) || string.Equals("Anonymous", policy, StringComparison.OrdinalIgnoreCase))
            return;
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            if (transformContext.HttpContext.Items.TryGetValue(InnerJwtMiddleware.InnerJwtKey, out var value) &&
                value is string jwt)
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }
            return default;
        });
    });

var app = builder.Build();
app.UseChatFoundryObservability();

app.UseRouting();


app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/identity/connect/token", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.EnableBuffering();
    }
    await next(context);
});

app.UseCors("BlazorClientPolicy");

// SignalR WebSocket: браузер не отправляет Authorization header,
// поэтому копируем access_token из query string в header до OpenIddict-валидации.
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Request.Headers.Authorization) &&
        context.Request.Query.TryGetValue("access_token", out var token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<InnerJwtMiddleware>();

app.MapReverseProxy();

app.Run();

public partial class Program { }
