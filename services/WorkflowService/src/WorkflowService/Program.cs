using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using WorkflowService.Actions.Executors;
using WorkflowService.Actions.Factories;
using WorkflowService.Consumers;
using WorkflowService.Data;
using WorkflowService.Events;
using WorkflowService.Grpc;
using WorkflowService.Interfaces;
using WorkflowService.Repositories;
using WorkflowService.Services;
using WorkflowService.Utils;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddControllers();
services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = "http://identity:8080";
        opt.Audience = "workflow";
        opt.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

services.AddScoped<IWorkflowRepository, WorkflowRepository>();
services.AddScoped<IActionRepository, ActionRepository>();
services.AddScoped<ISessionRepository, SessionRepository>();
services.AddScoped<IBotRepository, BotRepository>();

services.AddScoped<ISessionResolver, SessionResolver>();
services.AddScoped<IActionFactory, ActionFactory>();

services.AddScoped<IActionExecutor, SendMessageActionExecutor>();
services.AddScoped<IActionExecutor, AskActionExecutor>();
services.AddScoped<IActionExecutor, StartActionExecutor>();
services.AddScoped<IActionExecutor, InputExecutor>();

services.AddScoped<IMessageSender, MessageSender>();

services.AddScoped<IActionExecutorFactory, ActionExecutorFactory>();
services.AddScoped<WorkflowGraphParser>();
services.AddScoped<WorkflowTextRenderer>();

services.AddPostgreSql<WorkflowDbContext>(builder.Configuration);

services.AddSingleton(new AdminClientConfig
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
        rider.AddConsumer<BotMessageConsumer>();
        rider.AddConsumer<ExecuteActionConsumer>();
        rider.AddConsumer<ActionCompletedConsumer>();
        
        rider.AddProducer<BotIncomingMessage>("bot.message.incoming");
        rider.AddProducer<BotOutgoingMessage>("bot.message.outgoing");
        rider.AddProducer<ExecuteActionCommand>("workflow.action.execute");
        rider.AddProducer<ActionCompletedEvent>("workflow.action.completed");
        
        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host("localhost:9092");

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

builder.Services.AddGrpc();


var app = builder.Build();

app.MapGrpcService<BotTokenGrpcService>();

app.MapControllers();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Workflow Service is running");

app.MapGet("/run", () => "Workflow executed")
    .RequireAuthorization();

app.Run();