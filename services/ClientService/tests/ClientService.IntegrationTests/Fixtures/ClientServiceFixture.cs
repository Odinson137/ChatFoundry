using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using ClientService.Data;

namespace ClientService.IntegrationTests.Fixtures;

public class ClientServiceFixture : BaseServiceFactory<Program, ClientDbContext>
{
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => true;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Workflow.Grpc.BotTokenService.BotTokenServiceClient>();
        services.MockGrpcClient<global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient>();
    }
}