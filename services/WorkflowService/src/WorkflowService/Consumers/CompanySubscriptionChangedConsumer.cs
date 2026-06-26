using MassTransit;
using Shared.Application.Events;
using Shared.Infrastructure.GraphQl;

namespace WorkflowService.Consumers;

public class CompanySubscriptionChangedConsumer(IGraphQlCacheService cacheService)
    : IConsumer<CompanySubscriptionChangedEvent>
{
    public async Task Consume(ConsumeContext<CompanySubscriptionChangedEvent> context)
    {
        var companyId = context.Message.CompanyId;
        
        await cacheService.EvictByTagsAsync(new[] 
        { 
            $"company:{companyId}:bots", 
            $"company:{companyId}:workflows" 
        }, context.CancellationToken);
    }
}
