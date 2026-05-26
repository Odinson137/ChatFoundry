using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatFoundry.TestInfrastructure.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var userId = Guid.Empty.ToString();
        var companyId = Guid.Empty.ToString();
        var scopes = "";

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring(7);
            var parts = token.Split(':');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0])) userId = parts[0];
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) companyId = parts[1];
            if (parts.Length > 2) scopes = parts[2];
        }
        else
        {
            var userIdHeader = Request.Headers["X-Test-User-Id"].ToString();
            var companyIdHeader = Request.Headers["X-Test-Company-Id"].ToString();
            var scopesHeader = Request.Headers["X-Test-Scopes"].ToString();

            if (!string.IsNullOrEmpty(userIdHeader)) userId = userIdHeader;
            if (!string.IsNullOrEmpty(companyIdHeader)) companyId = companyIdHeader;
            if (!string.IsNullOrEmpty(scopesHeader)) scopes = scopesHeader;
        }

        if (userId == Guid.Empty.ToString())
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        if (companyId != Guid.Empty.ToString())
        {
            claims.Add(new Claim("company_id", companyId));
        }

        if (!string.IsNullOrEmpty(scopes))
        {
            foreach (var scope in scopes.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim("scope", scope.Trim()));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
