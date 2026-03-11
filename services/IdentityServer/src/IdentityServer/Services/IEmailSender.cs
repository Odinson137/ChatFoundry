namespace IdentityServer.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
