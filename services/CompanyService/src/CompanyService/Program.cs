using CompanyService.Data;
using CompanyService.GraphQL;
using CompanyService.GraphQL.Mutations;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPostgreSql<CompanyDbContext>(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Query>();
builder.Services.AddScoped<CompanyMutation>();
builder.Services.AddScoped<CompanyMemberMutation>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<CompanyMutation>()
    .AddTypeExtension<CompanyMemberMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapGraphQL();

app.UseHttpsRedirection();

app.MapGet("/", () => "Company Service is running");

app.Run();
