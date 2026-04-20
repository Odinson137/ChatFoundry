using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MessengerHubService.Consumers;
using MessengerHubService.Data;
using MessengerHubService.GraphQL;
using MessengerHubService.Hubs;
using MessengerHubService.Interfaces;
using MessengerHubService.Repositories;
using MessengerHubService.Services;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.GraphQl;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddControllers();
builder.Services.AddHttpContextAccessor();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

services.AddPostgreSql<MessengerHubDbContext>(builder.Configuration);

services.AddRedisCache(builder.Configuration, "CacheSettings");

services.AddScoped<ILiveChatSessionRepository, LiveChatSessionRepository>();
services.AddScoped<LiveChatService>();

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
                "messenger-hub-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<LiveChatRequestedConsumer>(context);
                });

            cfg.TopicEndpoint<BotIncomingMessage>(
                "bot.message.incoming",
                "messenger-hub-service",
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
app.MapGet("/", () => "Messenger Hub Service is running");

app.Run();
