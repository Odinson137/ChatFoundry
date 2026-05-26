using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Factories;
using ChatFoundry.TestInfrastructure.Extensions;
using WorkflowService.Data;

namespace WorkflowService.IntegrationTests.Fixtures;

public class WorkflowServiceFixture : BaseServiceFactory<Program, WorkflowDbContext>
{
    protected override bool NeedsKafka => true;
    protected override bool NeedsRedis => true;

    protected override void ConfigureGrpcMocks(IServiceCollection services)
    {
        services.MockGrpcClient<global::Workflow.Grpc.Client.ClientAttributesService.ClientAttributesServiceClient>();
        services.MockGrpcClient<global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient>();
        services.MockGrpcClient<global::Scheduler.Grpc.SchedulerGrpcService.SchedulerGrpcServiceClient>();
    }
}