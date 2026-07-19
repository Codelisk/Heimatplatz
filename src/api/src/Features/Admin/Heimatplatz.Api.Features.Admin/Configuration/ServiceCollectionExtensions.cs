using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Admin.Configuration;

/// <summary>
/// DI-Registrierung fuer das Admin Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Admin Features
    /// </summary>
    public static IServiceCollection AddAdminFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.AddGeneratedServices();
        return services;
    }
}
