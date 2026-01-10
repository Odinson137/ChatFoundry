using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Infrastructure.GraphQl;

public abstract class BaseQuery
{
    protected readonly IHttpContextAccessor HttpContextAccessor;
    
    protected readonly Guid UserId;
    
    public BaseQuery(IHttpContextAccessor httpContextAccessor)
    {
        HttpContextAccessor = httpContextAccessor;
        var user = HttpContextAccessor.HttpContext?.User;
        
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        UserId = Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
    }

}