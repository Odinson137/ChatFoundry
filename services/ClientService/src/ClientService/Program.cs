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
using Shared.Infrastructure.DependencyInjection;
using Workflow.Grpc;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddHttpContextAccessor();
services.AddGrpc();

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

builder.Services.AddAuthorization();

builder.Services.AddScoped<IClientChannelRepository, ClientChannelRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();
builder.Services.AddScoped<IBotCompanyResolver, BotCompanyResolver>();

builder.Services.AddPostgreSql<ClientDbContext>(builder.Configuration);

builder.Services.AddGrpcClient<BotTokenService.BotTokenServiceClient>(o =>
{
    var address = builder.Configuration["Services:WorkflowServiceUrl"];
    o.Address = new Uri(address ?? "http://workflow-service:8081");
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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.MapGrpcService<ClientService.Grpc.ClientAttributesGrpcService>();

app.UseHttpsRedirection();

app.MapGet("/", () => "Client Service is running");

app.Run();

