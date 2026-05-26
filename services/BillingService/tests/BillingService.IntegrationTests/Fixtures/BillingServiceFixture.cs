using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using BillingService.Data;

namespace BillingService.IntegrationTests.Fixtures;

public class BillingServiceFixture : BaseServiceFactory<Program, BillingDbContext>
{
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => false;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
    }
}