using Confluent.Kafka;
using File.Grpc;
using MassTransit;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Application.Events;
using TelegramService.Consumers;
using TelegramService.Interfaces;
using TelegramService.Services;
using TelegramService.Options;
using Workflow.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection(TelegramOptions.SectionName));

var services = builder.Services;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
services.AddScoped<ITelegramClient, TelegramClient>();
services.AddScoped<IBotTokenProvider, GrpcBotTokenProvider>();
services.AddScoped<IFileSignedUrlProvider, FileSignedUrlProvider>();

services.AddLogging();

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        var settings = options.SerializerSettings;

        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.MissingMemberHandling = MissingMemberHandling.Ignore;
        settings.DefaultValueHandling = DefaultValueHandling.Ignore;
        
        settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });

var kafkaConnectionString = builder.Configuration.GetConnectionString("Kafka");
builder.Services.AddSingleton(new AdminClientConfig
{
    BootstrapServers = kafkaConnectionString
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
        rider.AddProducer<TelegramSetWebhookEvent>("telegram.set-webhook");
        
        rider.AddConsumer<SendTelegramMessageConsumer>();
        rider.AddConsumer<SetTelegramWebhookConsumer>();

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);

            cfg.TopicEndpoint<BotOutgoingMessage>(
                "bot.message.outgoing",
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

builder.Services.AddGrpcClient<BotTokenService.BotTokenServiceClient>(o =>
{
    var address = builder.Configuration["Services:WorkflowServiceUrl"];
    o.Address = new Uri(address);
});

builder.Services.AddGrpcClient<File.Grpc.FileService.FileServiceClient>(o =>
{
    var address = builder.Configuration["Services:FileServiceUrl"];
    o.Address = new Uri(address);
});


var app = builder.Build();

app.MapGet("/", () => "TelegramService!");

//app.UseHttpsRedirection();
app.MapControllers();

var test = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramClient>();

//await test.SetWebhookAsync("https://mongoose-needed-partially.ngrok-free.app/telegramhook/7e56b86d-4fc9-4c71-aab5-5e9a5ea1b9f0", default);
app.Run();

