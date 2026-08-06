using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Ein Baustein des Widget-Katalogs. Neue Widget-Art = neue Implementierung
/// (+ Renderer je Frontend) - die Registrierung erfolgt explizit in
/// AddDashboardsFeature (nicht via [Service]/TryAdd, damit IEnumerable&lt;&gt; alle
/// Implementierungen erhaelt), der KI-Prompt-Katalog und der Validator kennen
/// die neue Art damit automatisch.
/// </summary>
public interface IDashboardWidgetResolver
{
    /// <summary>Widget-Art aus DashboardWidgetKinds (z.B. "property-list")</summary>
    string Kind { get; }

    /// <summary>Selbstbeschreibung fuer den KI-Prompt-Katalog (DashboardCatalogPromptBuilder)</summary>
    WidgetDescriptor Descriptor { get; }

    /// <summary>
    /// Fail-closed-Bereinigung eines KI-gelieferten Widgets: normalisiert Werte,
    /// kappt Limits, verwirft Unbekanntes. Liefert die bereinigte Instanz oder null
    /// (Widget wird komplett verworfen); Begruendungen wandern in warnings.
    /// Die Orts-Aufloesung (Locations -&gt; MunicipalityIds) passiert danach zentral
    /// im DashboardDefinitionValidator.
    /// </summary>
    DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings);

    /// <summary>
    /// Daten-Ebene: loest die Query des Widgets in-process ueber die bestehenden
    /// Mediator-Requests auf und liefert den anzeigefertigen Payload.
    /// </summary>
    Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken);
}

/// <summary>
/// Selbstbeschreibung einer Widget-Art. Wird zur Laufzeit in den KI-Prompt
/// gerendert - Katalog und Validator koennen so nie auseinanderlaufen.
/// </summary>
/// <param name="Kind">Widget-Art (kind-Wert im JSON)</param>
/// <param name="Purpose">Ein Satz: wofuer das Widget da ist (deutsch, fuer den Prompt)</param>
/// <param name="Details">Query-/Options-Hinweise fuer die KI (unterstuetzte Felder, erlaubte Werte, Defaults)</param>
public record WidgetDescriptor(
    string Kind,
    string Purpose,
    string Details
);
