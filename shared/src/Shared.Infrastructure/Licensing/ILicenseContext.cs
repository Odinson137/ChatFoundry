namespace Shared.Infrastructure.Licensing;

/// <summary>Effective limits for self-hosted deployments (from license JWT).</summary>
public interface ILicenseContext
{
    bool IsSelfHosted { get; }
    int? MaxClients { get; }
    int? MaxBots { get; }
    DateTimeOffset? ExpiresAt { get; }
}
