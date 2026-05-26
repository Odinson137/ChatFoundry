using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using FileService.Data;

namespace FileService.IntegrationTests.Fixtures;

public class FileServiceFixture : BaseServiceFactory<Program, FileDbContext>
{
    protected override bool NeedsKafka => false;
    protected override bool NeedsRedis => false;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
    }
}