using CompanyService.Data;
using CompanyService.GraphQL;
using CompanyService.GraphQL.Mutations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Infrastructure.GraphQl;
using Microsoft.IdentityModel.Tokens;
using Shared.Grpc.Identity;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddPostgreSql<CompanyDbContext>(builder.Configuration);

services.AddHttpContextAccessor();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

services.AddGrpc();
services.AddGrpcClient<UserCompanyService.UserCompanyServiceClient>(o =>
{
    var address = builder.Configuration["IdentityService:GrpcAddress"] ?? "http://identity-service:8081";
    o.Address = new Uri(address);
});

services.AddScoped<Query>();
services.AddScoped<CompanyMutation>();
services.AddScoped<CompanyMemberMutation>();
services.AddScoped<InvitationMutation>();

services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<CompanyMutation>()
    .AddTypeExtension<CompanyMemberMutation>()
    .AddTypeExtension<InvitationMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.MapGrpcService<CompanyService.Grpc.CompanyRegistrationGrpcService>();

app.MapGet("/", () => "Company Service is running");

app.Run();
