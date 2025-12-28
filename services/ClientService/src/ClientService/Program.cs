using ClientService.Consumers;
using ClientService.Interfaces;
using ClientService.Repositories;
using Confluent.Kafka;
using MassTransit;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using WorkflowService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddScoped<IClientChannelRepository, ClientChannelRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

builder.Services.AddPostgreSql<ClientDbContext>(builder.Configuration);

builder.Services.AddSingleton(new AdminClientConfig
{
    BootstrapServers = "localhost:9092"
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
            cfg.Host("localhost:9092");

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

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
// }

app.UseHttpsRedirection();

app.MapGet("/", () => "Client Service is running");

app.Run();

