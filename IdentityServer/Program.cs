using IdentityServer.Data;
using IdentityServer.Entities;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Shared.Infrastructure.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddPostgreSql<IdentityDbContext>(builder.Configuration);

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt => {
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireDigit = false;
        opt.Password.RequiredLength = 2;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddOpenIddict()
    .AddCore(options => {
        options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>();
    })
    .AddServer(options => {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowPasswordFlow();
        options.AllowClientCredentialsFlow();
        options.AcceptAnonymousClients(); // demo only
        options.RegisterScopes("api");
        options.UseAspNetCore().EnableTokenEndpointPassthrough();
    })
    .AddValidation(options => {
        options.UseLocalServer();
        options.UseAspNetCore();
    });


builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
});


var app = builder.Build();


app.UseDeveloperExceptionPage();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

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
            Permissions = {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api"
            }
        });
    }
}

app.Run();