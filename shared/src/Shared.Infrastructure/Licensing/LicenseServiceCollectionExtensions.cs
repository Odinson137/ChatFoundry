using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Licensing;

public static class LicenseServiceCollectionExtensions
{
    public static IServiceCollection AddChatFoundryLicense(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LicenseOptions>(configuration.GetSection(LicenseOptions.SectionName));
        var mode = configuration.GetValue<LicenseMode?>($"{LicenseOptions.SectionName}:Mode") ?? LicenseMode.Cloud;
        if (mode == LicenseMode.SelfHosted)
            services.AddSingleton<ILicenseContext, SelfHostedLicenseContextStub>();
        else
            services.AddSingleton<ILicenseContext, CloudLicenseContext>();
        return services;
    }
}
