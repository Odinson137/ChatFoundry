using System.Security.Cryptography;
using System.Text;
using BillingService.Data;
using BillingService.Entities;

namespace BillingService.Services;

public class LicenseGenerationService(BillingDbContext db)
{
    public async Task<(string KeyMaterial, Guid Id)> CreateEnterpriseLicenseAsync(
        Guid? companyId,
        DateTime? expiresAt,
        CancellationToken ct = default)
    {
        var raw = $"{Guid.NewGuid():N}{DateTime.UtcNow.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        var entity = new LicenseKey
        {
            CompanyId = companyId,
            KeyHash = hash[..64],
            Tier = "enterprise",
            ExpiresAt = expiresAt,
            IsRevoked = false,
            Metadata = null
        };
        db.LicenseKeys.Add(entity);
        await db.SaveChangesAsync(ct);

        return (raw, entity.Id);
    }
}
