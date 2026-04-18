namespace BillingService.Enums;

public enum ChangePlanResult
{
    NoChange = 0,
    UpgradeApplied = 1,
    DowngradeScheduled = 2,
    ClearedPending = 3,
    InsufficientBalance = 4
}
