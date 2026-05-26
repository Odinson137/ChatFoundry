using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using NotificationService.Data;

namespace NotificationService.IntegrationTests.Fixtures;

public class NotificationServiceFixture : BaseServiceFactory<Program, NotificationDbContext>
{
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => true;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Workflow.Grpc.Client.ClientAttributesService.ClientAttributesServiceClient>();
    }
}