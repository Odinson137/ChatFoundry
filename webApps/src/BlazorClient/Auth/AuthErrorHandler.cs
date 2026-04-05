using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using BlazorClient.Configuration;
using BlazorClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorClient.Auth;

/// <summary>
/// При ответе 401 пробует обновить токен через refresh_token; при неудаче — очищает хранилище и перенаправляет на страницу входа.
/// </summary>
public class AuthErrorHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;
    private readonly IServiceProvider _serviceProvider;

    public AuthErrorHandler(
        ILocalStorageService localStorage,
        NavigationManager navigation,
        IServiceProvider serviceProvider)
    {
        _localStorage = localStorage;
        _navigation = navigation;
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

        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Console.WriteLine("[AuthErrorHandler] No refresh token in localStorage, redirecting to login");
            await ClearAuthAndRedirect();
            return response;
        }

        Console.WriteLine($"[AuthErrorHandler] Attempting token refresh...");

        var tokenEndpoint = GetTokenEndpoint(request);
        Console.WriteLine($"[AuthErrorHandler] Token endpoint: {tokenEndpoint}");

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("scope", ApiEndpoints.OAuthScopes),
            })
        };

        var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
        if (!refreshResponse.IsSuccessStatusCode)
        {
            var errorBody = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[AuthErrorHandler] Refresh failed: {refreshResponse.StatusCode} — {errorBody}");
            await ClearAuthAndRedirect();
            return response;
        }

        var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            Console.WriteLine("[AuthErrorHandler] Refresh response has no access token");
            await ClearAuthAndRedirect();
            return response;
        }

        Console.WriteLine($"[AuthErrorHandler] Refresh successful, got new access token. Has new refresh token: {!string.IsNullOrEmpty(tokenResponse.RefreshToken)}");

        await _localStorage.SetItemAsync("authToken", tokenResponse.AccessToken);
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            await _localStorage.SetItemAsync("refreshToken", tokenResponse.RefreshToken);

        var authStateProvider = (ApiAuthenticationStateProvider)_serviceProvider.GetRequiredService<AuthenticationStateProvider>();
        authStateProvider.MarkUserAsAuthenticated(tokenResponse.AccessToken);

        var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var h in request.Headers)
            if (!string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                retryRequest.Headers.TryAddWithoutValidation(h.Key, h.Value);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        if (contentBytes != null && contentHeaders != null)
        {
            var retryContent = new ByteArrayContent(contentBytes);
            foreach (var h in contentHeaders)
                retryContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
            retryRequest.Content = retryContent;
        }

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static Uri GetTokenEndpoint(HttpRequestMessage request)
    {
        var authority = request.RequestUri?.GetLeftPart(UriPartial.Authority)
            ?? ApiEndpoints.Api.TrimEnd('/');
        return new Uri(new Uri(authority), "identity/connect/token");
    }

    private async Task ClearAuthAndRedirect()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("userEmail");

        var uri = new Uri(_navigation.Uri);
        if (!uri.AbsolutePath.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
            _navigation.NavigateTo("/login", forceLoad: true);
    }
}
