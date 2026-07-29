using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Properties.Services;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Handlers;

/// <summary>
/// Verwirft die In-Memory-Location-Hierarchie, wenn der Conditional-GET-Handler
/// eine inhaltliche Aenderung am Locations-Endpoint gemeldet hat. Der naechste
/// Zugriff laedt dann den frischen Stand aus dem Mediator-Cache.
/// </summary>
public sealed class StammdatenChangedEventHandler(ILocationService locationService)
    : IEventHandler<StammdatenChangedEvent>
{
    public Task Handle(StammdatenChangedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (@event.Path.StartsWith("/api/locations", StringComparison.OrdinalIgnoreCase))
            locationService.InvalidateCache();

        return Task.CompletedTask;
    }
}
