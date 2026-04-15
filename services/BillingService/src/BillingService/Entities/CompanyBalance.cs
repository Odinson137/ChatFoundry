using Shared.Domain.Entities;

namespace BillingService.Entities;

public class CompanyBalance : EntityBase
{
    public Guid CompanyId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USDT";
}
