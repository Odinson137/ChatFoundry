using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;

namespace TelegramService.IntegrationTests.Fixtures;

public class TelegramServiceFixture : StatelessServiceFactory<Program>
{
    protected override bool NeedsPostgres => false;
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => true;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Workflow.Grpc.BotTokenService.BotTokenServiceClient>();
        services.MockGrpcClient<global::File.Grpc.FileService.FileServiceClient>();
    }
}