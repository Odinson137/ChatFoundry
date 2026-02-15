using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorClient.Auth;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public ApiAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt")));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        var parts = token.Split('.');
        // JWE (encrypted token) has 5 parts — клиент не может расшифровать, не парсим
        if (parts.Length == 5)
            return [new Claim(ClaimTypes.Name, "User")];

        if (parts.Length != 3)
            return [new Claim(ClaimTypes.Name, "User")];

        try
        {
            var payload = parts[1];
            var jsonBytes = ParseBase64UrlWithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            if (keyValuePairs != null && keyValuePairs.TryGetValue(ClaimTypes.Name, out var name) && name != null)
                return [new Claim(ClaimTypes.Name, name.ToString()!)];
        }
        catch
        {
            // невалидный или нестандартный payload — считаем пользователя аутентифицированным без имени
        }

        return [new Claim(ClaimTypes.Name, "User")];
    }

    /// <summary>
    /// Декодирует base64url (JWT использует его в segment'ах). Стандартный Base64 не подходит — даёт Format_BadBase64Char.
    /// </summary>
    private static byte[] ParseBase64UrlWithoutPadding(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}