namespace Shared.Infrastructure.Licensing;

public class LicenseOptions
{
    public const string SectionName = "License";

    public LicenseMode Mode { get; set; } = LicenseMode.Cloud;
    /// <summary>RSA public key PEM for validating self-hosted license JWT (optional).</summary>
    public string? PublicKeyPem { get; set; }
    /// <summary>JWT license string (optional). Same as env CHATFOUNDRY_LICENSE.</summary>
    public string? LicenseToken { get; set; }
}
