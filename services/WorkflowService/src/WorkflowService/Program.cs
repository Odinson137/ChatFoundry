using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.GraphQl;
using Workflow.Grpc.Client;
using WorkflowService.Actions.Executors;
using WorkflowService.Actions.Factories;
using WorkflowService.Consumers;
using WorkflowService.Data;
using WorkflowService.Events;
using WorkflowService.GraphQL;
using WorkflowService.GraphQL.Mutations;
using WorkflowService.Grpc;
using WorkflowService.Interfaces;
using WorkflowService.Models;
using WorkflowService.Repositories;
using WorkflowService.Services;
using WorkflowService.Utils;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddControllers();
services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

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

services.AddScoped<IWorkflowRepository, WorkflowRepository>();
services.AddScoped<IActionRepository, ActionRepository>();
services.AddScoped<ISessionRepository, SessionRepository>();
services.AddScoped<IBotRepository, BotRepository>();
services.AddScoped<IChannelRepository, ChannelRepository>();

services.AddScoped<ISessionResolver, SessionResolver>();
services.AddScoped<IActionFactory, ActionFactory>();

services.AddScoped<IActionExecutor, SendMessageActionExecutor>();
services.AddScoped<IActionExecutor, AskActionExecutor>();
services.AddScoped<IActionExecutor, StartActionExecutor>();
services.AddScoped<IActionExecutor, InputExecutor>();
services.AddScoped<IActionExecutor, SetAttributeActionExecutor>();
services.AddScoped<IActionExecutor, HttpRequestActionExecutor>();
services.AddScoped<IActionExecutor, AIGenerateActionExecutor>();
services.AddScoped<IActionExecutor, SendMediaActionExecutor>();
services.AddScoped<IActionExecutor, SubWorkflowActionExecutor>();

services.AddScoped<IMessageSender, MessageSender>();
services.AddScoped<IOpenAiService, OpenAiService>();

services.AddScoped<IActionExecutorFactory, ActionExecutorFactory>();
services.AddScoped<WorkflowGraphParser>();
services.AddScoped<WorkflowTextRenderer>();
services.AddScoped<IVariableService, VariableService>();


services.AddHttpClient();

services.Configure<WorkflowService.Options.OpenAiOptions>(
    builder.Configuration.GetSection(WorkflowService.Options.OpenAiOptions.SectionName));
services.Configure<WorkflowService.Options.FileServiceOptions>(
    builder.Configuration.GetSection(WorkflowService.Options.FileServiceOptions.SectionName));

services.AddHttpClient<FileUrlResolver>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkflowService.Options.FileServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
});
services.AddScoped<IFileUrlResolver>(sp => sp.GetRequiredService<FileUrlResolver>());

services.AddPostgreSql<WorkflowDbContext>(builder.Configuration);

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
        rider.AddConsumer<BotMessageConsumer>();
        rider.AddConsumer<ExecuteActionConsumer>();
        rider.AddConsumer<ActionCompletedConsumer>();
        rider.AddProducer<TelegramSetWebhookEvent>("telegram.set-webhook");
        
        rider.AddProducer<BotIncomingMessage>("bot.message.incoming");
        rider.AddProducer<BotOutgoingMessage>("bot.message.outgoing");
        rider.AddProducer<ExecuteActionCommand>("workflow.action.execute");
        rider.AddProducer<ActionCompletedEvent>("workflow.action.completed");
        
        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);

            cfg.TopicEndpoint<BotIncomingMessage>(
                "bot.message.incoming",
                "workflow-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<BotMessageConsumer>(context);
                });
            cfg.TopicEndpoint<ExecuteActionCommand>(
                "workflow.action.execute",
                "workflow-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<ExecuteActionConsumer>(context);
                });
            cfg.TopicEndpoint<ActionCompletedEvent>(
                "workflow.action.completed",
                "workflow-service",
                e =>
                {
                    e.CreateIfMissing();
                    e.ConfigureConsumer<ActionCompletedConsumer>(context);
                });
        });
    });
});

services.AddGrpc();

services.AddGrpcClient<ClientAttributesService.ClientAttributesServiceClient>(o =>
{
    o.Address = new Uri("http://client-service:8081");
})
.AddStandardResilienceHandler();

services.AddRedisCache(builder.Configuration, "CacheSettings");
services.AddScoped<ClientAttributesGrpcClient>();
services.AddScoped<IClientAttributesGrpcClient>(sp => new CachingClientAttributesGrpcClient(
    sp.GetRequiredService<ClientAttributesGrpcClient>(),
    sp.GetRequiredService<IDistributedCache>(),
    sp.GetRequiredService<IOptions<Shared.Infrastructure.Options.FoundryRedisCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingClientAttributesGrpcClient>>()));

services.AddScoped<Query>();
services.AddScoped<BotMutation>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<BotMutation>()
    .AddTypeExtension<ChannelMutation>()
    .AddTypeExtension<BotWorkflowMutation>()
    .AddType<MessengerChannelType>()
    .AddType<WorkflowService.GraphQL.Types.BotWorkflowType>()
    .AddProjections() 
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapGrpcService<BotTokenGrpcService>();

app.MapControllers();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Workflow Service is running");

app.MapGet("/run", () => "Workflow executed")
    .RequireAuthorization();

app.MapGraphQL();

app.Run();