using System.Text;
using Heimatplatz.Api;
using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Fail-closed-Validierungspipeline fuer KI-Definitionen: Schema-Version pruefen,
/// Texte kappen, Widgets ueber die Resolver-Registry bereinigen (Unbekanntes wird
/// VERWORFEN, nie durchgereicht), Orte serverseitig aufloesen. Erst was hier
/// durchkommt, wird gespeichert und erreicht je einen Renderer.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DashboardDefinitionValidator(
    IEnumerable<IDashboardWidgetResolver> resolvers,
    IMediator mediator,
    IOptions<DashboardOptions> options,
    ILogger<DashboardDefinitionValidator> logger
)
{
    public const string NoWidgetsMessage =
        "Ihr Wunsch konnte noch nicht in eine Übersicht übersetzt werden - " +
        "beschreiben Sie bitte etwas konkreter, was Sie sehen möchten.";

    /// <summary>
    /// Validiert und bereinigt eine geparste KI-Definition. <paramref name="viewType"/>
    /// erzwingt die Ansichts-Regeln fail-closed (Liste = genau EIN property-list-Widget
    /// in voller Breite). Wirft <see cref="DashboardValidationException"/>
    /// (nutzerfreundlich), wenn am Ende kein einziges gueltiges Widget uebrig bleibt.
    /// Warnungen (verworfene Teile) werden geloggt - auf Warning-Level, Prod loggt Info nicht.
    /// </summary>
    public async Task<DashboardDefinition> ValidateAsync(DashboardDefinition definition, string viewType, CancellationToken cancellationToken)
    {
        if (definition.SchemaVersion != DashboardDefinition.CurrentSchemaVersion)
            throw new DashboardValidationException(
                "Die Übersicht konnte nicht erstellt werden (unerwartetes Format). Bitte versuchen Sie es erneut.");

        var warnings = new List<string>();
        var resolverByKind = resolvers.ToDictionary(r => r.Kind, StringComparer.OrdinalIgnoreCase);

        definition.Title = NormalizeText(definition.Title, 120) ?? "Meine Übersicht";
        definition.Intro = NormalizeText(definition.Intro, 300);
        definition.UnsupportedWishes = (definition.UnsupportedWishes ?? [])
            .Select(w => NormalizeText(w, 200))
            .Where(w => w is not null)
            .Select(w => w!)
            .Take(10)
            .ToList();

        var sanitized = new List<DashboardWidget>();
        foreach (var widget in (definition.Widgets ?? []).Take(options.Value.Limits.MaxWidgets))
        {
            var kind = widget.Kind?.Trim().ToLowerInvariant() ?? "";
            if (!resolverByKind.TryGetValue(kind, out var resolver))
            {
                warnings.Add($"Unbekannte Widget-Art verworfen: {widget.Kind}");
                continue;
            }

            widget.Kind = resolver.Kind;
            var clean = resolver.Sanitize(widget, warnings);
            if (clean is not null)
                sanitized.Add(clean);
        }

        if ((definition.Widgets?.Count ?? 0) > options.Value.Limits.MaxWidgets)
            warnings.Add($"Definition auf {options.Value.Limits.MaxWidgets} Widgets gekappt.");

        // Listen-Ansicht: genau EIN Listen-Widget in voller Breite - alles andere
        // fliegt (die KI bekommt fuer Listen ohnehin nur diesen Katalog angeboten)
        if (viewType == DashboardViewTypes.List)
        {
            var dropped = sanitized.Where(w => w.Kind != DashboardWidgetKinds.PropertyList).ToList();
            foreach (var widget in dropped)
                warnings.Add($"Listen-Ansicht: Widget {widget.Kind} verworfen.");
            sanitized.RemoveAll(w => w.Kind != DashboardWidgetKinds.PropertyList);

            if (sanitized.Count > 1)
            {
                warnings.Add($"Listen-Ansicht: {sanitized.Count - 1} zusaetzliche Listen-Widgets verworfen.");
                sanitized.RemoveRange(1, sanitized.Count - 1);
            }

            if (sanitized.Count == 1)
            {
                sanitized[0].Size = DashboardWidgetSizes.Full;
                // Eine seitenfuellende Liste vertraegt mehr Eintraege als ein Widget -
                // der Resolver hat vorher schon seinen Widget-Default (6) gesetzt,
                // deshalb wird genau der angehoben (explizite KI-Werte bleiben)
                sanitized[0].Query ??= new DashboardPropertyQuery();
                if (sanitized[0].Query!.Limit is null or PropertyListWidgetResolver.DefaultLimit)
                    sanitized[0].Query!.Limit = 12;
            }
        }

        // Personalisierte Detail-Overlay: Sections + Felder fail-closed gegen den Feld-Katalog
        definition.Detail = DashboardFieldCatalog.NormalizeDetail(definition.Detail, warnings);

        await ResolveLocationsAsync(sanitized, definition.UnsupportedWishes, warnings, cancellationToken);

        // Ids immer neu vergeben (stabil, eindeutig) - die Daten-Antwort matcht dagegen
        for (var i = 0; i < sanitized.Count; i++)
            sanitized[i].Id = $"w{i + 1}";

        definition.Widgets = sanitized;

        if (warnings.Count > 0)
            logger.LogWarning("[Dashboards] Validierung hat Teile der KI-Definition verworfen: {Warnings}",
                string.Join(" | ", warnings));

        if (sanitized.Count == 0)
            throw new DashboardValidationException(NoWidgetsMessage);

        return definition;
    }

    /// <summary>
    /// Loest die Freitext-Orte aller Widget-Queries ueber die Locations-Hierarchie auf
    /// (ein Abruf fuer die ganze Definition). Bezirkstreffer = alle Gemeinden des
    /// Bezirks. Unaufloesbare Orte wandern nach UnsupportedWishes; ein Widget, dessen
    /// Orte KOMPLETT unaufloesbar sind, wird verworfen (es wuerde sonst ungefiltert
    /// "alles" zeigen und damit luegen).
    /// </summary>
    private async Task ResolveLocationsAsync(
        List<DashboardWidget> widgets,
        List<string> unsupportedWishes,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var allLocationNames = widgets
            .Where(w => w.Query?.Locations is { Count: > 0 })
            .SelectMany(w => w.Query!.Locations!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allLocationNames.Count == 0)
            return;

        var locations = await mediator.Request(new GetLocationsRequest(), cancellationToken);
        var resolved = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in allLocationNames)
        {
            var ids = ResolveLocationName(name, locations.Result);
            if (ids.Count > 0)
            {
                resolved[name] = ids;
            }
            else
            {
                var wish = $"Ort „{name}“ wurde nicht erkannt";
                if (!unsupportedWishes.Contains(wish))
                    unsupportedWishes.Add(wish);
            }
        }

        foreach (var widget in widgets.ToList())
        {
            var query = widget.Query;
            if (query?.Locations is not { Count: > 0 })
                continue;

            var ids = query.Locations
                .Where(resolved.ContainsKey)
                .SelectMany(l => resolved[l])
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                warnings.Add($"Widget {widget.Kind} verworfen: kein Ort aufloesbar ({string.Join(", ", query.Locations)})");
                widgets.Remove(widget);
                continue;
            }

            query.MunicipalityIds = ids;
            query.Locations = query.Locations.Where(resolved.ContainsKey).ToList();
        }
    }

    /// <summary>
    /// Ein Ortsname gegen die Hierarchie: erst Bezirke (breiter = naeher an der
    /// Suchabsicht), dann Gemeinden. Vergleich case-insensitiv mit Umlaut-Faltung
    /// (voecklabruck == Vöcklabruck) und ohne "Bezirk "/"Gemeinde "-Praefix.
    /// </summary>
    public static List<Guid> ResolveLocationName(string name, GetLocationsResponse locations)
    {
        var needle = FoldName(name);
        if (needle.Length == 0)
            return [];

        foreach (var province in locations.FederalProvinces)
        {
            foreach (var district in province.Districts)
            {
                if (FoldName(district.Name) == needle)
                    return district.Municipalities.Select(m => m.Id).ToList();
            }
        }

        var municipalityIds = new List<Guid>();
        foreach (var province in locations.FederalProvinces)
        {
            foreach (var district in province.Districts)
            {
                foreach (var municipality in district.Municipalities)
                {
                    if (FoldName(municipality.Name) == needle)
                        municipalityIds.Add(municipality.Id);
                }
            }
        }

        return municipalityIds;
    }

    /// <summary>Kleinschreibung, Umlaut-/ß-Faltung, "Bezirk "/"Gemeinde "-Praefix entfernen.</summary>
    public static string FoldName(string name)
    {
        var trimmed = name.Trim().ToLowerInvariant();
        foreach (var prefix in new[] { "bezirk ", "gemeinde ", "bezirk-", "gemeinde-" })
        {
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                trimmed = trimmed[prefix.Length..].Trim();
                break;
            }
        }

        var sb = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            switch (c)
            {
                case 'ä': sb.Append("ae"); break;
                case 'ö': sb.Append("oe"); break;
                case 'ü': sb.Append("ue"); break;
                case 'ß': sb.Append("ss"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string? NormalizeText(string? text, int maxLength)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
