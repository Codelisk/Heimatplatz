using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Immobilien.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImmobilienFeature(this IServiceCollection services)
    {
        // ViewModels and services are auto-registered via [Service] attribute
        // or through Navigation Extensions
        return services;
    }
}
