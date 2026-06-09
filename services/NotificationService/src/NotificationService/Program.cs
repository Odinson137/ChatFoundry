using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Consumers;
using NotificationService.Data;
using NotificationService.GraphQL;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using NotificationService.Repositories;
using NotificationService.Services;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.GraphQl;
using Workflow.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("notification-service");

var services = builder.Services;
services.AddControllers();
builder.Services.AddHttpContextAccessor();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (!path.StartsWithSegments("/hub"))
                    return Task.CompletedTask;

                // Prefer Authorization header (set by Gateway with decrypted inner JWT).
                // Only fall back to query string for direct connections without Gateway.
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) &&
                    authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

services.AddPostgreSql<NotificationDbContext>(builder.Configuration);

services.AddRedisCache(builder.Configuration, "CacheSettings");

services.AddGrpcClient<ClientAttributesService.ClientAttributesServiceClient>(o =>
{
    o.Address = new Uri("http://client-service:8081");
})
.AddStandardResilienceHandler();

services.AddScoped<ILiveChatSessionRepository, LiveChatSessionRepository>();
services.AddScoped<LiveChatService>();
services.AddScoped<IClientAttributesService, ClientChannelResolverService>();

services.AddSignalR();

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
        rider.AddConsumer<LiveChatRequestedConsumer>();
        rider.AddConsumer<IncomingMessageConsumer>();

        rider.AddProducer<BotOutgoingMessage>("bot.message.outgoing");
        rider.AddProducer<ActionCompletedEvent>("workflow.action.completed");

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);

            cfg.TopicEndpoint<LiveChatRequestedEvent>(
                "livechat.event",
                "notification-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<LiveChatRequestedConsumer>(context);
                });

            cfg.TopicEndpoint<BotIncomingMessage>(
                "bot.message.incoming",
                "notification-service",
                e =>
                {
                    e.ConfigureConsumer<IncomingMessageConsumer>(context);
                });
        });
    });
});

services.AddScoped<Query>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<LiveChatMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();
app.UseChatFoundryObservability();

using (var scope = app.Services.CreateScope())
{
    var liveChatService = scope.ServiceProvider.GetRequiredService<LiveChatService>();
    await liveChatService.RepopulateRedisFromDbAsync(CancellationToken.None);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<LiveChatHub>("/hub/livechat");
app.MapGraphQL();
app.MapGet("/", () => "Notification Service is running");

app.Run();


public partial class Program { }
