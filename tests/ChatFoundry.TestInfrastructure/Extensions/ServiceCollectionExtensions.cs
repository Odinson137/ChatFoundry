using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ChatFoundry.TestInfrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection MockGrpcClient<TClient>(this IServiceCollection services)
        where TClient : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TClient)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        var mock = Substitute.For<TClient>();
        services.AddSingleton(mock);

        return services;
    }

    public static IServiceCollection RemoveService<TService>(this IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
        return services;
    }
}
