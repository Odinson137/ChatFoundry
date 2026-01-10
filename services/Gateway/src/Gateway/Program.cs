using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8080";
        //options.Audience = "gateway";
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
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();