using BlazorClient;
using BlazorClient.Auth;
using BlazorClient.Interfaces;
using BlazorClient.Services;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.BaseAddress;
if (!baseAddress.Contains("localhost") && !baseAddress.Contains("127.0.0.1"))
{
    var uri = new Uri(baseAddress);
    BlazorClient.Configuration.ApiEndpoints.Api = $"{uri.Scheme}://api.{uri.Host}";
}

builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddScoped<AuthErrorHandler>();
builder.Services.AddScoped<LanguageHeaderHandler>();
builder.Services.AddScoped(sp =>
{
    var langHandler = sp.GetRequiredService<LanguageHeaderHandler>();
    var authHandler = sp.GetRequiredService<AuthErrorHandler>();
    langHandler.InnerHandler = authHandler;
    authHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(langHandler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<IWorkflowApiClient, WorkflowApiClient>();
builder.Services.AddScoped<IClientApiClient, ClientApiClient>();
builder.Services.AddScoped<IIdentityApiClient, IdentityApiClient>();
builder.Services.AddScoped<ICompanyApiClient, CompanyApiClient>();
builder.Services.AddScoped<IFileApiClient, FileApiClient>();
builder.Services.AddScoped<IWorkflowSchemaService, WorkflowSchemaService>();
builder.Services.AddScoped<IBillingApiClient, BillingApiClient>();
builder.Services.AddScoped<INotificationApiClient, NotificationApiClient>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<LiveChatStateService>();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

var host = builder.Build();

var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
var cultureName = await localStorage.GetItemAsync<string>("culture");

if (string.IsNullOrEmpty(cultureName))
{
    try
    {
        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        var languages = await jsRuntime.InvokeAsync<string[]>("eval", "Array.from(navigator.languages || [navigator.language || navigator.userLanguage])");

        var isRussianOrBelarussian = languages != null && languages.Any(lang =>
            lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ||
            lang.StartsWith("be", StringComparison.OrdinalIgnoreCase));

        cultureName = isRussianOrBelarussian ? "ru-RU" : "en-US";
    }
    catch
    {
        cultureName = "ru-RU";
    }
}

var culture = new System.Globalization.CultureInfo(cultureName);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();