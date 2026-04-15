using Shared.Domain.Entities;

namespace BillingService.Entities;

public class LicenseKey : EntityBase
{
    public Guid? CompanyId { get; set; }
    public string KeyHash { get; set; } = null!;
    public string Tier { get; set; } = "enterprise";
    public int? MaxClients { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? Metadata { get; set; }
}
