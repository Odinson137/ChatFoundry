using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8080";
        options.Audience = "gateway";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("workflow", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("scope", "workflow");
    });

    options.AddPolicy("client", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("scope", "client");
    });

    options.AddPolicy("telegram", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("scope", "telegram");
    });

    options.AddPolicy("identity", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("scope", "identity");
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