using CompanyService.Data;
using CompanyService.GraphQL;
using CompanyService.GraphQL.Mutations;
using Shared.Grpc.Identity;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPostgreSql<CompanyDbContext>(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddGrpc();
builder.Services.AddGrpcClient<UserCompanyService.UserCompanyServiceClient>(o =>
{
    var address = builder.Configuration["IdentityService:GrpcAddress"] ?? "http://identity-service:8081";
    o.Address = new Uri(address);
});

builder.Services.AddScoped<Query>();
builder.Services.AddScoped<CompanyMutation>();
builder.Services.AddScoped<CompanyMemberMutation>();
builder.Services.AddScoped<InvitationMutation>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<CompanyMutation>()
    .AddTypeExtension<CompanyMemberMutation>()
    .AddTypeExtension<InvitationMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapGraphQL();
app.MapGrpcService<CompanyService.Grpc.CompanyRegistrationGrpcService>();

app.UseHttpsRedirection();

app.MapGet("/", () => "Company Service is running");

app.Run();
