using IdentityServer.Entities;
using Microsoft.AspNetCore.Identity;
using Shared.Infrastructure.GraphQl;

namespace IdentityServer.GraphQL;

public class MeQuery(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    : BaseGraphQl(httpContextAccessor)
{
    public async Task<MeUserType?> GetMe(CancellationToken cancellationToken = default)
    {
        if (UserId == Guid.Empty)
            return null;

        var user = await userManager.FindByIdAsync(UserId.ToString());
        if (user == null)
            return null;

        return new MeUserType
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            CreatedAt = user.CreatedAt
        };
    }
}
