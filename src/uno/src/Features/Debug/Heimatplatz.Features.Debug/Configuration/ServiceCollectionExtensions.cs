using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Debug.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDebugFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();
        return services;
    }
}
