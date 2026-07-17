using Heimatplatz.Maui.ApiClient.Generated;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Sync;

/// <summary>
/// Wird nach einem Delta-Sync publiziert, der lokale Immobilien-Caches veraendert hat.
/// ViewModels patchen damit ihre sichtbaren Listen in-place (ersetzen/entfernen),
/// ohne komplette Seiten neu zu laden.
/// </summary>
/// <param name="ChangedProperties">Aktuelle Listendaten neuer und geaenderter Immobilien</param>
/// <param name="CreatedIds">Ids neu hinzugekommener Immobilien (Teilmenge von ChangedProperties)</param>
/// <param name="DeletedIds">Ids geloeschter Immobilien</param>
/// <param name="FullResync">True wenn alle Immobilien-Caches verworfen wurden (Delta nicht moeglich)</param>
public record PropertyDataSyncedEvent(
    IReadOnlyList<PropertyListItemDto> ChangedProperties,
    IReadOnlyList<Guid> CreatedIds,
    IReadOnlyList<Guid> DeletedIds,
    bool FullResync
) : IEvent;
