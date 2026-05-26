using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.Options;
using TelegramService.Consumers;
using TelegramService.Interfaces;
using TelegramService.Services;
using TelegramService.Options;
using Workflow.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("telegram-service");

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection(TelegramOptions.SectionName));

var services = builder.Services;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
services.AddScoped<ITelegramClient, TelegramClient>();
services.AddScoped<GrpcBotTokenProvider>();
services.AddScoped<FileSignedUrlProvider>();
services.AddRedisCache(builder.Configuration, "CacheSettings");
services.AddScoped<IBotTokenProvider>(sp => new CachingBotTokenProvider(
    sp.GetRequiredService<GrpcBotTokenProvider>(),
    sp.GetRequiredService<IDistributedCache>(),
    sp.GetRequiredService<IOptions<FoundryRedisCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingBotTokenProvider>>()));
services.AddScoped<IFileSignedUrlProvider>(sp => new CachingFileSignedUrlProvider(
    sp.GetRequiredService<FileSignedUrlProvider>(),
    sp.GetRequiredService<IDistributedCache>(),
    sp.GetRequiredService<IOptions<FoundryRedisCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingFileSignedUrlProvider>>()));

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

services.AddHttpClient("FileServiceRest", client =>
{
    var restUrl = builder.Configuration["Services:FileServiceRestUrl"] ?? "http://file-service:8080";
    client.BaseAddress = new Uri(restUrl.TrimEnd('/') + "/");
});
services.AddScoped<IMediaUploader, MediaUploader>();


var app = builder.Build();
app.UseChatFoundryObservability();

app.MapGet("/", () => "Telegram Service is running");


app.MapControllers();

var test = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramClient>();


app.Run();



public partial class Program { }
