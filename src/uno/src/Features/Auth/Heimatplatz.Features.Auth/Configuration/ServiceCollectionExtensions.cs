using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Auth.Configuration;

/// <summary>
/// DI-Registrierung fuer das Auth Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Services des Auth Features
    /// </summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        // ViewModels und Services werden automatisch via [Service] Attribut registriert
        // oder hier explizit registriert falls noetig

        return services;
    }
}
