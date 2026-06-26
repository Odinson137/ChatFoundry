using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using CompanyService.Data;

namespace CompanyService.IntegrationTests.Fixtures;

public class CompanyServiceFixture : BaseServiceFactory<Program, CompanyDbContext>
{
    protected override bool NeedsKafka => false;
    protected override bool NeedsRedis => true;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Shared.Grpc.Identity.UserCompanyService.UserCompanyServiceClient>();
        services.MockGrpcClient<global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient>();
    }
}