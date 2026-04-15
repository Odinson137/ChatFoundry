using Shared.Domain.Entities;

namespace BillingService.Entities;

public class UsageRecord : EntityBase
{
    public Guid CompanyId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int ClientsCount { get; set; }
    public int AiExecutionsUsed { get; set; }
    public int AiBuilderRequestsUsed { get; set; }
}
