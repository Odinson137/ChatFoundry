namespace Shared.Infrastructure.Licensing;

public interface ILicenseContext
{
    bool IsSelfHosted { get; }
    int? MaxClients { get; }
    int? MaxBots { get; }
    DateTimeOffset? ExpiresAt { get; }
}
