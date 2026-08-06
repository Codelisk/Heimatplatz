using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.Dashboards.Contracts.Models;

/// <summary>
/// Anzeigefertige Daten eines Widgets (Daten-Ebene, keine KI beteiligt).
/// Bewusst nullable-typisierte Payload-Felder statt Polymorphie: der
/// OpenAPI-Generator (MAUI-Client) bildet das sauber ab, und neue Widget-Arten
/// sind additive, nicht-brechende Erweiterungen. Je nach Kind ist genau ein
/// Payload-Feld gesetzt; highlight und new-listings nutzen PropertyList mit.
/// Fail-soft: ein fehlgeschlagenes Widget liefert Success=false + Error,
/// die uebrigen Widgets bleiben davon unberuehrt.
/// </summary>
public record WidgetDataDto(
    string WidgetId,
    string Kind,
    bool Success,
    string? Error,
    PropertyListWidgetData? PropertyList = null,
    StatRowWidgetData? StatRow = null,
    MapWidgetData? Map = null,
    TextNoteWidgetData? TextNote = null,
    ChartWidgetData? Chart = null
);

/// <summary>Trefferliste (property-list, highlight, new-listings). Total = Treffer der Filterung insgesamt.</summary>
public record PropertyListWidgetData(
    List<PropertyListItemDto> Properties,
    int Total
);

/// <summary>Kennzahl-Kacheln (stat-row). Label und Value kommen anzeigefertig vom Server (Backend-First).</summary>
public record StatRowWidgetData(
    List<StatTileDto> Tiles
);

/// <summary>Eine Kennzahl-Kachel. Key = Tile-Schluessel aus den Options (fuer Test-/Automations-IDs).</summary>
public record StatTileDto(
    string Key,
    string Label,
    string Value
);

/// <summary>Karten-Pins (map). Gleiche Semantik wie GetPropertyMapPinsResponse (Privacy-Jitter serverseitig).</summary>
public record MapWidgetData(
    List<PropertyMapPinDto> Pins,
    int Total,
    int WithoutCoordinates,
    bool Truncated
);

/// <summary>Statischer KI-Text (text-note) - steht in der Definition, hier nur durchgereicht.</summary>
public record TextNoteWidgetData(
    string Text
);

/// <summary>
/// Server-gerendertes Diagramm (price-chart): die API malt das Bild per SkiaSharp,
/// die Frontends zeigen nur ein Bild an - null Client-Bundle, in Web und MAUI
/// identisch. Beide Theme-Varianten als data:-URIs (kein zweiter Request, keine
/// Auth-Probleme mit img-Tags); der Client blendet passend zum Theme um.
/// </summary>
/// <param name="ChartKind">Diagramm-Art aus den Widget-Options ("priceHistogram" | "newPerWeek")</param>
/// <param name="AltText">Barrierefreie Kurzbeschreibung des Diagramminhalts</param>
/// <param name="Caption">Optionale Bildunterschrift (z.B. Datenbasis)</param>
public record ChartWidgetData(
    string ChartKind,
    string ImageLight,
    string ImageDark,
    string AltText,
    string? Caption
);
