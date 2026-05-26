using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;

namespace SchedulerService.IntegrationTests.Fixtures;

public class SchedulerServiceFixture : StatelessServiceFactory<Program>
{
    protected override bool NeedsPostgres => true;
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => false;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Workflow.Grpc.Client.ClientAttributesService.ClientAttributesServiceClient>();
    }
}