using BillingService.Enums;
using Shared.Domain.Entities;

namespace BillingService.Entities;

public class BalanceTransaction : EntityBase
{
    public Guid CompanyId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public Guid? ReferenceId { get; set; }
}
