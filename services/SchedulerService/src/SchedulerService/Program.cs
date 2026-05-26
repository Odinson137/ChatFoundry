using MassTransit;
using Quartz;
using Shared.Infrastructure.DependencyInjection;
using Scheduler.Grpc;
using SchedulerService;
using SchedulerService.Grpc;
using SchedulerService.Jobs;
using Shared.Application.Events;
using Workflow.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("scheduler-service");

var services = builder.Services;
var kafkaConnectionString = builder.Configuration.GetConnectionString("Kafka");
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Quartz schema initialization (must run before Quartz starts)
services.AddHostedService(sp =>
    new QuartzSchemaInitializer(postgresConnectionString, sp.GetRequiredService<ILogger<QuartzSchemaInitializer>>()));

// Quartz with PostgreSQL job store
services.AddQuartz(q =>
{
    q.SchedulerId = "chatfoundry-scheduler";
    q.SchedulerName = "ChatFoundry Scheduler";
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.UsePersistentStore(s =>
    {
        s.UseProperties = true;
        s.UsePostgres(postgresConnectionString);
        s.PerformSchemaValidation = false;
        s.UseNewtonsoftJsonSerializer();
    });

    q.AddJob<WaitJob>(j => j.StoreDurably());
    q.AddJob<TimerStartJob>(j => j.StoreDurably());
});

services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

// MassTransit Kafka producer
services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    x.AddRider(rider =>
    {
        rider.AddProducer<ActionCompletedEvent>("workflow.action.completed");
        rider.AddProducer<BotIncomingMessage>("bot.message.incoming");

        rider.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaConnectionString);
        });
    });
});

// gRPC
services.AddGrpc();

services.AddGrpcClient<SchedulerGrpcService.SchedulerGrpcServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:8081");
});
// Override with actual address when running in Docker
// In Docker: http://scheduler-service:8081

services.AddGrpcClient<ClientAttributesService.ClientAttributesServiceClient>(o =>
{
    o.Address = new Uri("http://client-service:8081");
})
.AddStandardResilienceHandler();

var app = builder.Build();
app.UseChatFoundryObservability();

app.MapGrpcService<SchedulerGrpcServiceImpl>();

app.MapGet("/", () => "Scheduler Service is running");

app.Run();


public partial class Program { }
