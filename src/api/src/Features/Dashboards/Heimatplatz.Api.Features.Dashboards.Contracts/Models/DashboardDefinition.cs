namespace Heimatplatz.Api.Features.Dashboards.Contracts.Models;

/// <summary>
/// Die vom KI-Designer erzeugte, serverseitig validierte Beschreibung einer
/// persoenlichen Uebersicht ("Meine Uebersicht"). Server-Driven UI: die KI liefert
/// niemals Code, nur diese Struktur aus Bausteinen des festen Widget-Katalogs -
/// Web und MAUI sind reine Renderer. Gespeichert als JSON am UserDashboard;
/// Clients muessen tolerant lesen (unbekannte Kinds ueberspringen, unbekannte
/// Felder ignorieren), damit API und Frontends unabhaengig deployen koennen.
/// </summary>
public class DashboardDefinition
{
    /// <summary>Aktuelle Schema-Version; der Validator akzeptiert nur diese von der KI</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Von der KI vergebener Titel der Uebersicht (vom Nutzer umbenennbar)</summary>
    public string Title { get; set; } = "";

    /// <summary>Optionaler Einleitungssatz unter dem Titel</summary>
    public string? Intro { get; set; }

    public List<DashboardWidget> Widgets { get; set; } = [];

    /// <summary>
    /// Personalisierte Detailansicht (Overlay beim Klick auf ein Inserat in der
    /// Uebersicht): Sektions-Reihenfolge + Fakten-Felder nach Kundenwunsch.
    /// null = Standard-Aufbau. Die oeffentliche Detailseite bleibt unberuehrt.
    /// </summary>
    public DashboardDetailSpec? Detail { get; set; }

    /// <summary>
    /// Filterleisten-Verhalten der Listen-Ansicht. null = Standard (die
    /// Startseiten-Filterleiste erscheint, vorbelegt mit der KI-Query).
    /// Nur gesetzt, wenn der Kunde AUSDRUECKLICH etwas anderes wuenscht.
    /// </summary>
    public DashboardFilterSpec? Filter { get; set; }

    /// <summary>
    /// Wuensche des Nutzers, die der Widget-Katalog (noch) nicht erfuellen kann.
    /// Werden dem Nutzer ehrlich angezeigt statt erfunden - und sind zugleich die
    /// Produkt-Roadmap aus echten Nutzerwuenschen.
    /// </summary>
    public List<string> UnsupportedWishes { get; set; } = [];
}

/// <summary>
/// Aufbau der personalisierten Detail-Overlay. Sections bestimmen Reihenfolge
/// und Auswahl der Bloecke, Fields die Fakten-Zeilen innerhalb von "facts"
/// (Schluessel aus dem Feld-Katalog, serverseitig fail-closed bereinigt).
/// </summary>
public class DashboardDetailSpec
{
    /// <summary>Aus <see cref="DashboardDetailSections"/>; leer/null = Standard-Reihenfolge</summary>
    public List<string>? Sections { get; set; }

    /// <summary>Geordnete Feld-Schluessel fuer den Fakten-Block; leer/null = Standard-Felder</summary>
    public List<string>? Fields { get; set; }
}

/// <summary>
/// Filterleisten-Verhalten der Listen-Ansicht ("nur wenn der Kunde explizit
/// einen anderen Filter wuenscht"). v1: nur Ausblenden; weitere Anpassungen
/// (z.B. gesperrte Filter) sind additive Erweiterungen.
/// </summary>
public class DashboardFilterSpec
{
    /// <summary>true = Filterleiste komplett ausblenden</summary>
    public bool? Hidden { get; set; }
}

/// <summary>Bloecke der Detail-Overlay</summary>
public static class DashboardDetailSections
{
    public const string Gallery = "gallery";
    public const string Facts = "facts";
    public const string Description = "description";
    public const string Features = "features";
    public const string Contact = "contact";
    public const string Map = "map";
}

/// <summary>
/// Ein Baustein der Uebersicht. Kind bestimmt den Renderer (Widget-Katalog),
/// Query die Datenauswahl, Options die Darstellungsvariante.
/// </summary>
public class DashboardWidget
{
    /// <summary>Stabile Kennung innerhalb der Definition (w1, w2, ...); ordnet die Daten-Antwort zu</summary>
    public string Id { get; set; } = "";

    /// <summary>Widget-Art aus <see cref="DashboardWidgetKinds"/></summary>
    public string Kind { get; set; } = "";

    /// <summary>Semantische Groesse aus <see cref="DashboardWidgetSizes"/> - die Renderer mappen selbst auf Raster/Spalten</summary>
    public string Size { get; set; } = DashboardWidgetSizes.Full;

    /// <summary>Optionale Ueberschrift des Widgets</summary>
    public string? Title { get; set; }

    /// <summary>Datenauswahl fuer immobilienbasierte Widgets (text-note braucht keine)</summary>
    public DashboardPropertyQuery? Query { get; set; }

    public DashboardWidgetOptions? Options { get; set; }
}

/// <summary>
/// Einheitliche Datenauswahl aller immobilienbasierten Widgets - dieselben Achsen
/// wie PropertyQueryFilters im Properties-Feature. Locations liefert die KI als
/// Freitext (wie der Nutzer den Ort nennt); MunicipalityIds befuellt ausschliesslich
/// der serverseitige Validator (Locations-Aufloesung, Bezirk = alle Gemeinden).
/// </summary>
public class DashboardPropertyQuery
{
    /// <summary>"house" | "land" | "foreclosure" (leer = Haus + Grund; ZV nur auf expliziten Wunsch)</summary>
    public List<string>? Types { get; set; }

    /// <summary>"private" | "broker" (leer = alle Anbieter)</summary>
    public List<string>? Sellers { get; set; }

    /// <summary>Orts-/Bezirksnamen im Wortlaut des Nutzers; Aufloesung passiert serverseitig</summary>
    public List<string>? Locations { get; set; }

    /// <summary>Serverseitig aufgeloeste Gemeinde-IDs (vom Validator gesetzt, nie von der KI)</summary>
    public List<Guid>? MunicipalityIds { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    /// <summary>Flaeche in m² (Wohnflaeche, bei Grundstuecken Grundflaeche)</summary>
    public int? AreaMin { get; set; }

    public int? AreaMax { get; set; }

    public int? RoomsMin { get; set; }

    /// <summary>false = Neubauprojekte ausblenden (null/true = anzeigen, wie die Suche)</summary>
    public bool? IncludeNewBuild { get; set; }

    /// <summary>Volltextsuche (Titel, Beschreibung, Adresse, Gemeinde, Anbietername)</summary>
    public string? SearchText { get; set; }

    /// <summary>"newest" | "oldest" | "price-asc" | "price-desc" | "area-asc" | "area-desc"</summary>
    public string? Sort { get; set; }

    /// <summary>Maximale Trefferzahl des Widgets (Validator kappt auf das Options-Limit)</summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Darstellungsoptionen je Widget-Art ("nur die Daten, die der Nutzer sich wuenscht"
/// auf Darstellungs-Ebene). Bewusst feste Varianten statt freier Feldauswahl,
/// damit jedes generierte Dashboard im Marken-Look bleibt.
/// </summary>
public class DashboardWidgetOptions
{
    /// <summary>property-list: "grid" | "list" | "minimal" (Preset; fields uebersteuert die Feldauswahl)</summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Listen-Widgets: geordnete Feld-Schluessel aus dem Feld-Katalog ("welche
    /// Werte will der Kunde sehen") - z.B. ["foto","titel","ort","preis","wohnflaeche"].
    /// leer/null = das Preset aus Variant bestimmt die Felder.
    /// </summary>
    public List<string>? Fields { get; set; }

    /// <summary>stat-row: Auswahl aus "total" | "newLast7Days" | "minPrice" | "medianPrice" | "maxPrice"</summary>
    public List<string>? Tiles { get; set; }

    /// <summary>text-note: der statische Text (steht in der Definition, keine Live-Daten)</summary>
    public string? Text { get; set; }

    /// <summary>price-chart: "priceHistogram" (Preisverteilung) | "newPerWeek" (Neuzugaenge pro Woche)</summary>
    public string? Chart { get; set; }
}

/// <summary>
/// Die zwei Ansichts-Typen: "dashboard" = freie Widget-Komposition,
/// "list" = eine seitenfuellende Immobilienliste (genau EIN property-list-Widget
/// in voller Breite + optionale Detail-Spec; der Validator erzwingt das fail-closed).
/// Der Nutzer waehlt den Typ beim Erstellen - er steuert KI-Prompt,
/// Validierungsregeln und das Rendering.
/// </summary>
public static class DashboardViewTypes
{
    public const string Dashboard = "dashboard";
    public const string List = "list";
}

/// <summary>Widget-Arten des Katalogs v1. Neue Arten = neuer Resolver im Dashboards-Feature + Renderer je Frontend.</summary>
public static class DashboardWidgetKinds
{
    public const string PropertyList = "property-list";
    public const string StatRow = "stat-row";
    public const string Map = "map";
    public const string Highlight = "highlight";
    public const string NewListings = "new-listings";
    public const string TextNote = "text-note";
    public const string PriceChart = "price-chart";
}

/// <summary>Semantische Widget-Groessen; Web mappt auf 12-Spalten-Grid (s=4, m=6, l=8, full=12), Mobil/Phone stapelt</summary>
public static class DashboardWidgetSizes
{
    public const string S = "s";
    public const string M = "m";
    public const string L = "l";
    public const string Full = "full";
}
