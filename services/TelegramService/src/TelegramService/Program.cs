using Confluent.Kafka;
using MassTransit;
using Shared.Application.Events;
using TelegramService.Consumers;
using TelegramService.Interfaces;
using TelegramService.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
services.AddScoped<ITelegramClient, TelegramClient>();
services.AddLogging();

builder.Services.AddSingleton(new AdminClientConfig
{
    BootstrapServers = "localhost:9092"
});

services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    x.AddRider(rider =>
    {
        rider.AddProducer<BotIncomingMessage>("bot.message.incoming");
        rider.AddProducer<ActionCompletedEvent>("workflow.action.completed");
        
        rider.AddConsumer<SendTelegramMessageConsumer>();
        rider.AddConsumer<SetTelegramWebhookConsumer>();

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host("localhost:9092");

            cfg.TopicEndpoint<TelegramSendMessageEvent>(
                "telegram.send-message",
                "telegram-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<SendTelegramMessageConsumer>(context);
                });

            cfg.TopicEndpoint<TelegramSetWebhookEvent>(
                "telegram.set-webhook",
                "telegram-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<SetTelegramWebhookConsumer>(context);
                });
        });
    });
});

var app = builder.Build();

app.MapGet("/", () => "TelegramService!");

//app.UseHttpsRedirection();
app.MapControllers();

var test = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramClient>();

//await test.SetWebhookAsync("https://mongoose-needed-partially.ngrok-free.app/telegramhook/7e56b86d-4fc9-4c71-aab5-5e9a5ea1b9f0", default);
app.Run();

