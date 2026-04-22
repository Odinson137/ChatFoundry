using System.Net.Http.Json;
using Blazored.LocalStorage;
using BlazorClient.Configuration;
using BlazorClient.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorClient.Auth;

public class TokenRefreshService : ITokenRefreshService
{
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;

    public TokenRefreshService(
        ILocalStorageService localStorage,
        NavigationManager navigation)
    {
        _localStorage = localStorage;
        _navigation = navigation;
    }

    public async Task<string?> TryRefreshTokenAsync()
    {
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenUrl = $"{ApiEndpoints.Api}/identity/connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "client"),
            new KeyValuePair<string, string>("client_secret", "secret"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("scope", ApiEndpoints.OAuthScopes),
        });

        using var client = new HttpClient();
        var response = await client.PostAsync(tokenUrl, content);

        if (!response.IsSuccessStatusCode)
            return null;

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            return null;

        await _localStorage.SetItemAsync("authToken", tokenResponse.AccessToken);
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            await _localStorage.SetItemAsync("refreshToken", tokenResponse.RefreshToken);

        return tokenResponse.AccessToken;
    }

    public async Task ClearAuthAndRedirectAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("userEmail");

        var uri = new Uri(_navigation.Uri);
        if (!uri.AbsolutePath.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
            _navigation.NavigateTo("/login", forceLoad: true);
    }
}
