namespace Shared.Infrastructure.Licensing;

public class LicenseOptions
{
    public const string SectionName = "License";

    public LicenseMode Mode { get; set; } = LicenseMode.Cloud;
    public string? PublicKeyPem { get; set; }
    public string? LicenseToken { get; set; }
}
