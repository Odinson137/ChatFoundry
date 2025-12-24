using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;

public class WorkflowServiceFactory : IDisposable
{
    public IServiceProvider Services { get; private set; }
    public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

    public WorkflowServiceFactory()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Добавляем сервисы для тестов
        // builder.Services.AddScoped<ITestProbe, TestProbe>();
        //
        // // Настройка Kafka для тестов
        // builder.Services.AddKafkaConsumer(options =>
        // {
        //     options.BootstrapServers = "localhost:9092";
        //     options.GroupId = "test-group";
        //     options.Topic = "bot-messages";
        // });

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