using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        // Services werden automatisch via [Service] Attribut registriert
        return services;
    }
}
