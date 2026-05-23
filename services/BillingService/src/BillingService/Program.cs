using BillingService.Consumers;
using BillingService.Data;
using BillingService.GraphQL;
using BillingService.Grpc;
using BillingService.Options;
using BillingService.Services;
using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.GraphQl;

var builder = WebApplication.CreateBuilder(args);
builder.AddChatFoundryObservability("billing-service");
var services = builder.Services;

services.AddControllers();
services.AddHttpContextAccessor();
services.AddHttpClient();

services.Configure<HeleketOptions>(builder.Configuration.GetSection(HeleketOptions.SectionName));

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
    });

services.AddAuthorization();

services.AddPostgreSql<BillingDbContext>(builder.Configuration);

services.AddScoped<BillingAccountService>();
services.AddScoped<HeleketPaymentService>();

services.AddHostedService<BillingCycleService>();

var kafkaConnectionString = builder.Configuration.GetConnectionString("Kafka");
if (!string.IsNullOrEmpty(kafkaConnectionString))
{
    services.AddSingleton(new AdminClientConfig { BootstrapServers = kafkaConnectionString });

    services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });

        x.AddRider(rider =>
        {
            rider.AddConsumer<ActionCompletedBillingConsumer>();

            rider.UsingKafka((context, cfg) =>
            {
                cfg.Host(kafkaConnectionString);

                cfg.TopicEndpoint<ActionCompletedEvent>(
                    "workflow.action.completed",
                    "billing-service",
                    e =>
                    {
                        e.CreateIfMissing();
                        e.ConfigureConsumer<ActionCompletedBillingConsumer>(context);
                    });
            });
        });
    });
}

services.AddGrpc();

services.AddScoped<BillingQuery>();
services.AddScoped<BillingMutation>();

services
    .AddGraphQLServer()
    .AddQueryType<BillingService.GraphQL.Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<BillingQuery>()
    .AddTypeExtension<BillingMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();
app.UseChatFoundryObservability();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await BillingPlanSeedService.SeedPlansAsync(db);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<BillingQuotaGrpcService>();
app.MapGraphQL();

app.MapGet("/", () => "Billing Service is running");

await app.RunAsync();
