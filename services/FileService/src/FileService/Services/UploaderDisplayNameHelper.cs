using System.Security.Claims;

namespace FileService.Services;

public static class UploaderDisplayNameHelper
{
    public static string? FromPrincipal(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var name = user.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(email))
            return email.Trim();

        var genericName = user.FindFirstValue("name");
        return string.IsNullOrWhiteSpace(genericName) ? null : genericName.Trim();
    }
}
