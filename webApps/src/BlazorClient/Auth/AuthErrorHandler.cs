using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorClient.Auth;

public class AuthErrorHandler : DelegatingHandler
{
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly IServiceProvider _serviceProvider;

    public AuthErrorHandler(
        ITokenRefreshService tokenRefreshService,
        IServiceProvider serviceProvider)
    {
        _tokenRefreshService = tokenRefreshService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        byte[]? contentBytes = null;
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? contentHeaders = null;

        if (request.Content != null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentHeaders = request.Content.Headers.ToArray();
            var newContent = new ByteArrayContent(contentBytes);
            foreach (var h in contentHeaders)
                newContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
            request.Content = newContent;
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        Console.WriteLine($"[AuthErrorHandler] 401 received for {request.RequestUri}");

        var newToken = await _tokenRefreshService.TryRefreshTokenAsync();
        if (newToken == null)
        {
            Console.WriteLine("[AuthErrorHandler] Token refresh failed, redirecting to login");
            await _tokenRefreshService.ClearAuthAndRedirectAsync();
            return response;
        }

        Console.WriteLine("[AuthErrorHandler] Refresh successful, retrying request");

        var authStateProvider = (ApiAuthenticationStateProvider)_serviceProvider.GetRequiredService<AuthenticationStateProvider>();
        authStateProvider.MarkUserAsAuthenticated(newToken);

        var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var h in request.Headers)
            if (!string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                retryRequest.Headers.TryAddWithoutValidation(h.Key, h.Value);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        if (contentBytes != null && contentHeaders != null)
        {
            var retryContent = new ByteArrayContent(contentBytes);
            foreach (var h in contentHeaders)
                retryContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
            retryRequest.Content = retryContent;
        }

        return await base.SendAsync(retryRequest, cancellationToken);
    }
}
