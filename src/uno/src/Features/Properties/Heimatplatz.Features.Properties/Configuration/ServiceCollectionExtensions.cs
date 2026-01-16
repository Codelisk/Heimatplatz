using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        // Properties feature uses Shiny.Mediator with generated HTTP requests
        // No additional services need to be registered
        return services;
    }
}
