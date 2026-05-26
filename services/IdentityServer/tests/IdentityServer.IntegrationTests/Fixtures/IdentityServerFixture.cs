using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using IdentityServer.Data;

namespace IdentityServer.IntegrationTests.Fixtures;

public class IdentityServerFixture : BaseServiceFactory<Program, IdentityDbContext>
{
    protected override bool NeedsKafka => false;
    protected override bool NeedsRedis => false;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
    }
}