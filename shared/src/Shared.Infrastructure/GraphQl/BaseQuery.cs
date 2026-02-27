using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.GraphQl;

public abstract class BaseGraphQl
{
    protected readonly IHttpContextAccessor HttpContextAccessor;

    protected readonly Guid UserId;
    protected readonly Guid? CompanyId;

    public BaseGraphQl(IHttpContextAccessor httpContextAccessor)
    {
        HttpContextAccessor = httpContextAccessor;
        var user = HttpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user?.FindFirstValue("sub");
        UserId = Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;

        var companyId = user?.FindFirstValue("company_id");
        CompanyId = Guid.TryParse(companyId, out var cid) ? cid : null;
    }
}