using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

[Authorize]
public class LiveChatHub : Hub
{
    private const string CompanyGroupPrefix = "company_";

    public override async Task OnConnectedAsync()
    {
        var companyId = GetCompanyIdClaimValue(Context.User);
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{CompanyGroupPrefix}{companyId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = GetCompanyIdClaimValue(Context.User);
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{CompanyGroupPrefix}{companyId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GetCompanyGroupName(Guid? companyId) => $"{CompanyGroupPrefix}{companyId}";

    private static string? GetCompanyIdClaimValue(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst("company_id")?.Value
               ?? user.Claims.FirstOrDefault(c => c.Type.EndsWith("/company_id", StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
