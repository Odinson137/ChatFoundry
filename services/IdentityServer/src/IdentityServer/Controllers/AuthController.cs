using IdentityServer.Entities;
using IdentityServer.Models;
using IdentityServer.Options;
using IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServer.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ICompanyRegistrationClient companyClient,
    IEmailSender emailSender,
    IOptions<EmailConfirmationOptions> emailConfirmationOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and Password are required.");

        var hasInvite = !string.IsNullOrWhiteSpace(req.InviteToken);
        var hasCompanyName = !string.IsNullOrWhiteSpace(req.CompanyName);

        if (hasInvite == hasCompanyName)
            return BadRequest("Provide either InviteToken or CompanyName.");

        if (hasCompanyName)
            return await RegisterWithNewCompanyAsync(req);
        return await RegisterWithInviteAsync(req);
    }

    private async Task<IActionResult> RegisterWithNewCompanyAsync(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            Email = req.Email,
            UserName = req.Email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var (companyId, success) = await companyClient.CreateCompanyWithOwnerAsync(req.CompanyName!.Trim(), user.Id, HttpContext.RequestAborted);
        if (!success)
        {
            await userManager.DeleteAsync(user);
            return BadRequest("Failed to create company.");
        }

        user.CompanyId = companyId;
        await userManager.UpdateAsync(user);

        await SendConfirmationEmailAsync(user);
        return Ok(new { message = "User registered successfully. Please check your email to confirm your address." });
    }

    private async Task<IActionResult> RegisterWithInviteAsync(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            Email = req.Email,
            UserName = req.Email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var companyId = await companyClient.ConsumeInviteAsync(req.InviteToken!.Trim(), user.Id, HttpContext.RequestAborted);
        if (!companyId.HasValue)
            return BadRequest("Invalid or expired invitation.");

        user.CompanyId = companyId;
        await userManager.UpdateAsync(user);

        await SendConfirmationEmailAsync(user);
        return Ok(new { message = "User registered successfully. Please check your email to confirm your address." });
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        try
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = emailConfirmationOptions.Value.AppBaseUrl.TrimEnd('/');
            var confirmUrl = $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            var subject = "Подтвердите ваш email — ChatFoundry";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body>
<p>Здравствуйте!</p>
<p>Подтвердите ваш адрес электронной почты, перейдя по ссылке:</p>
<p><a href=""{confirmUrl}"">Подтвердить email</a></p>
<p>Если вы не регистрировались в ChatFoundry, проигнорируйте это письмо.</p>
</body>
</html>";
            await emailSender.SendEmailAsync(user.Email!, subject, htmlBody, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string? userId, [FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return Ok(new { succeeded = false, message = "Invalid or missing parameters." });

        if (!Guid.TryParse(userId, out _))
            return Ok(new { succeeded = false, message = "Invalid user id." });

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Ok(new { succeeded = false, message = "User not found." });

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
            return Ok(new { succeeded = true, message = "Email confirmed successfully." });
        return Ok(new { succeeded = false, message = "Invalid or expired confirmation link." });
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
            return Ok(new { message = "If an account exists and is not confirmed, a new email has been sent." });

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null || user.EmailConfirmed)
        {
            return Ok(new { message = "If an account exists and is not confirmed, a new email has been sent." });
        }

        try
        {
            await SendConfirmationEmailAsync(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend confirmation to {Email}", request.Email);
        }

        return Ok(new { message = "If an account exists and is not confirmed, a new email has been sent." });
    }
}
