using Microsoft.Extensions.Options;

namespace Shared.Infrastructure.Licensing;

/// <summary>Placeholder: wire RSA JWT validation and claims mapping for production self-hosted builds.</summary>
public sealed class SelfHostedLicenseContextStub : ILicenseContext
{
    public SelfHostedLicenseContextStub(IOptions<LicenseOptions> _)
    {
    }

    public bool IsSelfHosted => true;
    public int? MaxClients => null;
    public int? MaxBots => null;
    public DateTimeOffset? ExpiresAt => null;
}
