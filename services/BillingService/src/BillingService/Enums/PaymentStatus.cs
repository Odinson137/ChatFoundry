namespace BillingService.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Confirmed = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    WrongAmount = 5
}
