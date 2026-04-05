using IdentityServer.Data;
using IdentityServer.Entities;
using IdentityServer.GraphQL;
using IdentityServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Shared.Grpc.Company;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddGrpc();
services.AddHttpContextAccessor();

services.AddPostgreSql<IdentityDbContext>(builder.Configuration);

services.Configure<CompanyServiceOptions>(builder.Configuration.GetSection(CompanyServiceOptions.SectionName));
services.AddGrpcClient<CompanyRegistrationService.CompanyRegistrationServiceClient>(o =>
{
    var address = builder.Configuration["CompanyService:GrpcAddress"];
    o.Address = new Uri(address);
});
services.AddScoped<ICompanyRegistrationClient, CompanyRegistrationClient>();

services.AddDataProtection();

services.Configure<IdentityServer.Options.SmtpOptions>(builder.Configuration.GetSection(IdentityServer.Options.SmtpOptions.SectionName));
services.Configure<IdentityServer.Options.EmailConfirmationOptions>(builder.Configuration.GetSection(IdentityServer.Options.EmailConfirmationOptions.SectionName));
services.AddSingleton<IEmailSender, SmtpEmailSender>();

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
        
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AllowClientCredentialsFlow();

        
        
        options.RegisterScopes(
            "workflow",
            "client",
            "telegram",
            "identity",
            "file",
            "company",
            OpenIddictConstants.Scopes.OfflineAccess
        );

        var encryptionKeyBase64 = builder.Configuration["OpenIddict:EncryptionKey"]
            ?? throw new InvalidOperationException(
                "OpenIddict:EncryptionKey не задан. Задайте переменную окружения OpenIddict__EncryptionKey или значение в appsettings.");
        options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKeyBase64)));

        options.AddDevelopmentSigningCertificate();
        options.SetIssuer(new Uri("http://identity-service:8080/"));
        
        options.UseAspNetCore().EnableTokenEndpointPassthrough().DisableTransportSecurityRequirement();

        options.AddEventHandler<OpenIddictServerEvents.ProcessSignInContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                context.Principal!.SetAudiences("gateway");
                return default;
            });
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

services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

services.AddAuthorization();

services.AddScoped<MeQuery>();

services.AddGraphQLServer()
    .AddQueryType<MeQuery>();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGraphQL();
app.MapGrpcService<IdentityServer.Grpc.UserCompanyGrpcService>();

app.MapGet("/", () => "IdentityServer sample running");


using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = new ApplicationUser { UserName = "bob", Email = "bob@example.com", EmailConfirmed = true };
    var createResult = await userMgr.CreateAsync(user, "Pass123!");
    if (createResult.Succeeded)
    {
        var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyRegistrationClient>();
        var (companyId, success) = await companyClient.CreateCompanyWithOwnerAsync("Bob's Company", user.Id);
        if (success)
        {
            user.CompanyId = companyId;
            await userMgr.UpdateAsync(user);
        }
    }

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
                OpenIddictConstants.Permissions.Prefixes.Scope + "client",
                OpenIddictConstants.Permissions.Prefixes.Scope + "telegram",
                OpenIddictConstants.Permissions.Prefixes.Scope + "identity",
                OpenIddictConstants.Permissions.Prefixes.Scope + "file",
                OpenIddictConstants.Permissions.Prefixes.Scope + "company",
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",

                OpenIddictConstants.Permissions.ResponseTypes.Token,
            }
        });
    }
}

app.Run();