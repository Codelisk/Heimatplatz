using Heimatplatz.Core.DeepLink.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Core.DeepLink.Configuration;

/// <summary>
/// Extension methods for registering DeepLink services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DeepLink feature services to the service collection
    /// </summary>
    public static IServiceCollection AddDeepLinkFeature(this IServiceCollection services)
    {
        // DeepLinkService wird automatisch via [Service] Attribut registriert
        // Diese Methode dient als expliziter Entry Point fuer das Feature
        return services;
    }
}
