using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Properties.Handlers;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// HttpClient-basierte Services und Mediator-Handler des Properties-Features -
    /// die uebrigen Services registrieren sich ueber die Shiny-DI-Attribute selbst.
    /// </summary>
    public static IServiceCollection AddPropertiesFeature(this IServiceCollection services)
    {
        // Kurzes Timeout: der Tile-Probe entscheidet nur Stil vs. Fallback und
        // darf das Oeffnen der Karte nicht lange blockieren
        services.AddHttpClient<IMapStyleProvider, MapStyleProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(8));

        // Geaenderte Stammdaten (Conditional GET) -> In-Memory-Locations verwerfen
        services.AddSingleton<IEventHandler<StammdatenChangedEvent>, StammdatenChangedEventHandler>();
        return services;
    }
}
