using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Maui.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// HttpClient-basierte Services des Properties-Features - die uebrigen Services
    /// registrieren sich ueber die Shiny-DI-Attribute selbst.
    /// </summary>
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        // Kurzes Timeout: der Tile-Probe entscheidet nur Stil vs. Fallback und
        // darf das Oeffnen der Karte nicht lange blockieren
        services.AddHttpClient<IMapStyleProvider, MapStyleProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(8));
        return services;
    }
}
