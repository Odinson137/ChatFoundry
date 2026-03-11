using BlazorClient;
using BlazorClient.Auth;
using BlazorClient.Interfaces;
using BlazorClient.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthErrorHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthErrorHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

builder.Services.AddScoped<IWorkflowApiClient, WorkflowApiClient>();
builder.Services.AddScoped<IClientApiClient, ClientApiClient>();
builder.Services.AddScoped<IIdentityApiClient, IdentityApiClient>();
builder.Services.AddScoped<ICompanyApiClient, CompanyApiClient>();
builder.Services.AddScoped<IFileApiClient, FileApiClient>();
builder.Services.AddScoped<IWorkflowSchemaService, WorkflowSchemaService>();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

await builder.Build().RunAsync();