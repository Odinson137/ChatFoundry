using ClientService.Consumers;
using ClientService.Data;
using ClientService.GraphQL;
using ClientService.GraphQL.Mutations;
using ClientService.Interfaces;
using ClientService.Repositories;
using ClientService.Services;
using Shared.Infrastructure.GraphQl;
using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Events;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.Options;
using Workflow.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("client-service");
var services = builder.Services;

services.AddHttpContextAccessor();
services.AddGrpc();
services.AddLocalization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityService:JwtAuthority"] ?? "http://identity-service:8080";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["IdentityService:JwtIssuer"] ?? "http://identity-service:8080/",
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IClientChannelRepository, ClientChannelRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();
builder.Services.AddScoped<BotCompanyResolver>();
builder.Services.AddRedisCache(builder.Configuration, "CacheSettings");
builder.Services.AddGraphQlCaching(builder.Configuration);
builder.Services.AddScoped<IBotCompanyResolver>(sp => new CachingBotCompanyResolver(
    sp.GetRequiredService<BotCompanyResolver>(),
    sp.GetRequiredService<IDistributedCache>(),
    sp.GetRequiredService<IOptions<FoundryRedisCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingBotCompanyResolver>>()));

builder.Services.AddPostgreSql<ClientDbContext>(builder.Configuration);

builder.Services.AddGrpcClient<BotTokenService.BotTokenServiceClient>(o =>
{
    var address = builder.Configuration["Services:WorkflowServiceUrl"];
    o.Address = new Uri(address ?? "http://workflow-service:8081");
});

builder.Services.AddGrpcClient<global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient>(o =>
{
    var address = builder.Configuration["Services:BillingServiceGrpc"] ?? "http://billing-service:8081";
    o.Address = new Uri(address);
});

var kafkaConnectionString = builder.Configuration.GetConnectionString("Kafka");
builder.Services.AddSingleton(new AdminClientConfig
{
    BootstrapServers = kafkaConnectionString
});

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    const string groupName = "client-service";
    x.AddRider(rider =>
    {
        rider.AddConsumer<BotIncomingMessageConsumer>();
        rider.AddConsumer<BotOutgoingMessageConsumer>();

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);

            cfg.TopicEndpoint<BotIncomingMessage>(
                "bot.message.incoming",
                groupName,
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<BotIncomingMessageConsumer>(context);
                });

            cfg.TopicEndpoint<BotOutgoingMessage>(
                "bot.message.outgoing",
                groupName,
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<BotOutgoingMessageConsumer>(context);
                });
        });
    });
});

builder.Services.AddScoped<Query>();
builder.Services.AddScoped<ClientMutation>();
builder.Services.AddScoped<AttributeDefinitionMutation>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<ClientMutation>()
    .AddTypeExtension<AttributeDefinitionMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

var app = builder.Build();
app.UseChatFoundryObservability();

app.UseRouting();

var supportedCultures = new[] { "ru-RU", "en-US" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("ru-RU")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseAuthentication();
app.UseAuthorization();

app.UseGraphQlCaching();

app.MapGraphQL();
app.MapGrpcService<ClientService.Grpc.ClientAttributesGrpcService>();

app.UseHttpsRedirection();

app.MapGet("/", () => "Client Service is running");

app.Run();



public partial class Program { }
