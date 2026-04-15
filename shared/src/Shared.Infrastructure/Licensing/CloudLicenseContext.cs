namespace Shared.Infrastructure.Licensing;

public sealed class CloudLicenseContext : ILicenseContext
{
    public bool IsSelfHosted => false;
    public int? MaxClients => null;
    public int? MaxBots => null;
    public DateTimeOffset? ExpiresAt => null;
}
