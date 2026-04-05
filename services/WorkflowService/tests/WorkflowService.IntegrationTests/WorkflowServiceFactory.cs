using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;

public class WorkflowServiceFactory : IDisposable
{
    public IServiceProvider Services { get; private set; }
    public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

    public WorkflowServiceFactory()
    {
        var builder = WebApplication.CreateBuilder();
        
        
        
        
        
        
        
        
        
        
        

        var app = builder.Build();
        WebApplicationFactory = new WebApplicationFactory<Program>();
        Services = WebApplicationFactory.Services;
    }

    public HttpClient CreateClient()
    {
        return WebApplicationFactory.CreateClient();
    }

    public void Dispose()
    {
        WebApplicationFactory?.Dispose();
    }
}