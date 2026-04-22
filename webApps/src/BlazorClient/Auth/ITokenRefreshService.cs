namespace BlazorClient.Auth;

public interface ITokenRefreshService
{
    Task<string?> TryRefreshTokenAsync();
    Task ClearAuthAndRedirectAsync();
}
