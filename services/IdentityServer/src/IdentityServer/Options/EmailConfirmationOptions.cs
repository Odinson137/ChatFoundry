namespace IdentityServer.Options;

public class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    public string AppBaseUrl { get; set; } = "https://localhost:7555";
}
