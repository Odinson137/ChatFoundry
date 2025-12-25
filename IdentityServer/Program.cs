using IdentityServer.Data;
using IdentityServer.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddPostgreSql<IdentityDbContext>(builder.Configuration);

services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt =>
    {
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireDigit = false;
        opt.Password.RequiredLength = 2;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();


services.AddOpenIddict()
    .AddCore(options => { options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>(); })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        //options.AllowAuthorizationCodeFlow();
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AllowClientCredentialsFlow();
        
        //options.AcceptAnonymousClients(); // demo only
        options.RegisterScopes("workflow");
        
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore().EnableTokenEndpointPassthrough();
    });


services.AddAuthentication(options => { options.DefaultScheme = IdentityConstants.ApplicationScheme; });

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "IdentityServer sample running");


using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = new ApplicationUser { UserName = "bob", Email = "bob@example.com" };
    await userMgr.CreateAsync(user, "Pass123!");

    var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    if (await appManager.FindByClientIdAsync("client") == null)
    {
        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "client",
            ClientSecret = "secret",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,

                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + "workflow",
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",

                OpenIddictConstants.Permissions.ResponseTypes.Token,
            }
        });
    }
}

app.Run();