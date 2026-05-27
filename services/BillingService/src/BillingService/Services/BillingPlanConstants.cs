namespace BillingService.Services;

public static class BillingPlanConstants
{
    public static readonly Guid FreePlanId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid StarterPlanId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid ProPlanId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid BusinessPlanId = Guid.Parse("11111111-1111-1111-1111-111111111104");

    public const string QuotaClients = "clients";
    public const string QuotaBots = "bots";
    public const string QuotaTeamMembers = "team_members";
    public const string QuotaAiTokens = "ai_tokens";
}
