using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Abstractions;
using IdentityServer.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer.Controllers;

[ApiController]
public class TokenController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : ControllerBase
{
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("Invalid OpenIddict request.");

        if (request.IsPasswordGrantType())
        {
            var user =
                await userManager.FindByEmailAsync(request.Username) ??
                await userManager.FindByNameAsync(request.Username);

            if (user == null ||
                !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "Неверный email или пароль."
                });
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest(new
                {
                    error = "email_not_confirmed",
                    error_description = "Подтвердите адрес электронной почты. Проверьте почту или запросите новое письмо."
                });
            }

            var principal = await signInManager.CreateUserPrincipalAsync(user);

            principal.SetClaim(
                OpenIddictConstants.Claims.Subject,
                user.Id.ToString());
            if (user.CompanyId.HasValue)
                principal.SetClaim("company_id", user.CompanyId.Value.ToString());

            principal.SetScopes(request.GetScopes());

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            }

            return SignIn(
                principal,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!request.IsPasswordGrantType() && !request.IsRefreshTokenGrantType())
        {
            return BadRequest(new
            {
                error = "unsupported_grant_type",
                error_description = $"Grant type '{request.GrantType}' is not supported. Use 'password' or 'refresh_token'."
            });
        }

        if (request.IsRefreshTokenGrantType())
        {
            var authenticateResult =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var sub = authenticateResult.Principal!.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = sub != null ? await userManager.FindByIdAsync(sub) : null;

            var identity = new ClaimsIdentity(
                [
                    new Claim(OpenIddictConstants.Claims.Subject, sub ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, "refresh_user")
                ],
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (user?.CompanyId != null)
                identity.AddClaim(new Claim("company_id", user.CompanyId.Value.ToString()));

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            foreach (var claim in principal.Claims)
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type", error_description = "Unsupported grant type." });
    }
}