namespace IdentityServer.Options;

public class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    /// <summary>
    /// Base URL of the frontend app (e.g. https://localhost:7555) for confirmation links in emails.
    /// </summary>
    public string AppBaseUrl { get; set; } = "https://localhost:7555";
}
