using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: null);

var secretToken = builder.Configuration["Telegram:SecretToken"]
    ?? throw new InvalidOperationException(
        "Переменная окружения Telegram__SecretToken не найдена. " +
        "Убедитесь, что .env подключён в docker-compose для gateway-service.");

builder.Configuration["ReverseProxy:Routes:telegram-hook-route:Match:Headers:0:Values:0"] = secretToken;

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "BlazorClientPolicy",
        policy =>
        {
            policy.WithOrigins("https://localhost:7555")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8080";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://identity-service:8080/",
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
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
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();

app.UseCors("BlazorClientPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();