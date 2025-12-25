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
                return Forbid(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await signInManager.CreateUserPrincipalAsync(user);

            principal.SetClaim(
                OpenIddictConstants.Claims.Subject,
                user.Id.ToString());

            principal.SetScopes(request.GetScopes());

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            }

            return SignIn(
                principal,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var authenticateResult =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var identity = new ClaimsIdentity(
                [
                    new Claim(OpenIddictConstants.Claims.Subject,
                        authenticateResult.Principal!.GetClaim(OpenIddictConstants.Claims.Subject)!), 
                    new Claim(ClaimTypes.NameIdentifier, "refresh_user")
                ],
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest("Unsupported grant type.");
    }
}