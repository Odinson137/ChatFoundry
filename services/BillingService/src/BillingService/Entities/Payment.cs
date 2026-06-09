using BillingService.Enums;
using Shared.Domain.Entities;

namespace BillingService.Entities;

public class Payment : EntityBase
{
    public Guid CompanyId { get; set; }
    public string OrderId { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? AmountUsd { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Network { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? WebhookPayload { get; set; }
}
