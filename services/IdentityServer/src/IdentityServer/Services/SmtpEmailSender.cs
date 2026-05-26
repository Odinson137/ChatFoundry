using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IdentityServer.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IdentityServer.Options.SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<IdentityServer.Options.SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("SMTP not configured. Would send email to {To}, subject: {Subject}. Body length: {Length}",
                to, subject, htmlBody.Length);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromDisplayName ?? "ChatFoundry", _options.FromAddress ?? "noreply@localhost"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            var secureSocketOptions = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_options.Host!, _options.Port, secureSocketOptions, ct);
            if (!string.IsNullOrWhiteSpace(_options.UserName))
                await client.AuthenticateAsync(_options.UserName, _options.Password ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            _logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}, subject: {Subject}", to, subject);
            throw;
        }
    }
}
