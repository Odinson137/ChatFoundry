using ClientService.Consumers;
using ClientService.Data;
using ClientService.GraphQL;
using ClientService.GraphQL.Mutations;
using ClientService.Interfaces;
using ClientService.Repositories;
using Confluent.Kafka;
using MassTransit;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

services.AddGrpc();

builder.Services.AddScoped<IClientChannelRepository, ClientChannelRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();

builder.Services.AddPostgreSql<ClientDbContext>(builder.Configuration);

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

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<ClientMutation>()
    .AddMutationType<AttributeDefinitionMutation>()
    .AddProjections() 
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapGraphQL();
app.MapGrpcService<ClientService.Grpc.ClientAttributesGrpcService>();

app.UseHttpsRedirection();

app.MapGet("/", () => "Client Service is running");

app.Run();

