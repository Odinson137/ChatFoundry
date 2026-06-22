using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Infrastructure.Options;
using SmsService.Consumers;
using SmsService.Interfaces;
using SmsService.Models;
using SmsService.Services;
using Workflow.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("sms-service");

builder.Services.Configure<SmsOptions>(
    builder.Configuration.GetSection(SmsOptions.SectionName));

var services = builder.Services;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

services.AddRedisCache(builder.Configuration, "CacheSettings");
services.AddScoped<SmsSettingsProvider>();
services.AddScoped<ISmsSettingsProvider>(sp => new CachingSmsSettingsProvider(
    sp.GetRequiredService<SmsSettingsProvider>(),
    sp.GetRequiredService<IDistributedCache>(),
    sp.GetRequiredService<IOptions<FoundryRedisCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingSmsSettingsProvider>>()));

services.AddHttpClient();

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

var kafkaConnectionString = builder.Configuration.GetConnectionString("Kafka") ?? "localhost:9092";
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
        rider.AddConsumer<SendSmsMessageConsumer>();

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);

            cfg.TopicEndpoint<BotOutgoingMessage>(
                "bot.message.outgoing",
                "sms-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<SendSmsMessageConsumer>(context);
                });
        });
    });
});

builder.Services.AddGrpcClient<BotTokenService.BotTokenServiceClient>(o =>
{
    var address = builder.Configuration["Services:WorkflowServiceUrl"] ?? "http://workflow-service:8081";
    o.Address = new Uri(address);
});

var app = builder.Build();
app.UseChatFoundryObservability();

app.MapGet("/", () => "SMS Service is running");

app.MapControllers();

app.Run();

public partial class Program { }
