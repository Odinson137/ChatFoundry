using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;

namespace Gateway.IntegrationTests.Fixtures;

public class GatewayFixture : StatelessServiceFactory<Program>
{
    static GatewayFixture()
    {
        System.Environment.SetEnvironmentVariable("Telegram__SecretToken", "test_secret_token_123");
        System.Environment.SetEnvironmentVariable("OpenIddict__EncryptionKey", "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=");
    }

    protected override bool NeedsPostgres => false;
    protected override bool NeedsKafka => false;
    protected override bool NeedsRedis => false;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
    }
}