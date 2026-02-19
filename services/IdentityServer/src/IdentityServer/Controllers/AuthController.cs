using IdentityServer.Entities;
using IdentityServer.Models;
using IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ICompanyRegistrationClient companyClient) : ControllerBase
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
            EmailConfirmed = true
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

        return Ok(new { message = "User registered successfully" });
    }

    private async Task<IActionResult> RegisterWithInviteAsync(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            Email = req.Email,
            UserName = req.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var companyId = await companyClient.ConsumeInviteAsync(req.InviteToken!.Trim(), user.Id, HttpContext.RequestAborted);
        if (!companyId.HasValue)
            return BadRequest("Invalid or expired invitation.");

        user.CompanyId = companyId;
        await userManager.UpdateAsync(user);

        return Ok(new { message = "User registered successfully" });
    }
}
