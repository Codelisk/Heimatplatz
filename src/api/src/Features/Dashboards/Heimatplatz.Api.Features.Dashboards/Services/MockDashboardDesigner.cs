using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Dev-Provider ohne KI: liefert deterministisch eine Beispiel-Definition, die
/// ALLE Widget-Arten des Katalogs abdeckt (damit E2E-Tests jeden Renderer sehen).
/// Der konfigurierbare Delay macht den Async-Flow (Queued -> InProgress -> Finished)
/// realistisch testbar. Verfeinerungsrunden haengen eine text-note mit der
/// Anweisung an - so ist der Refine-Pfad im UI nachvollziehbar.
/// Die Ausgabe laeuft durch denselben Parser/Validator wie echte KI-Antworten.
/// </summary>
public class MockDashboardDesigner(
    IOptions<DashboardOptions> options,
    ILogger<MockDashboardDesigner> logger
) : IDashboardDesigner
{
    public async Task<string> DesignAsync(string request, string viewType, string? currentDefinitionJson, CancellationToken cancellationToken = default)
    {
        var delay = Math.Max(options.Value.MockDelaySeconds, 0);
        logger.LogWarning("[Dashboards] Mock-Designer aktiv (keine echte KI) - liefere Beispiel-Definition in {Delay}s", delay);
        if (delay > 0)
            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

        var current = DashboardDefinitionSerializer.DeserializeStored(currentDefinitionJson);
        var definition = current is not null
            ? BuildRefinedDefinition(current, request, viewType)
            : viewType == DashboardViewTypes.List
                ? BuildInitialListDefinition(request)
                : BuildInitialDefinition(request);

        return DashboardDefinitionSerializer.Serialize(definition);
    }

    /// <summary>Listen-Ansicht: genau EIN seitenfuellendes Listen-Widget + Detail-Spec.</summary>
    private static DashboardDefinition BuildInitialListDefinition(string request) => new()
    {
        Title = "Ihre Immobilienliste",
        Intro = $"Demo-Liste zu Ihrem Wunsch: „{Truncate(request, 120)}“ (Mock-Modus, ohne KI erstellt).",
        Detail = new DashboardDetailSpec
        {
            Sections = ["facts", "gallery", "description", "contact"],
            Fields = ["preis", "wohnflaeche", "grundflaeche", "zimmer", "ort", "anbieter"]
        },
        Widgets =
        [
            new DashboardWidget
            {
                Id = "w1", Kind = DashboardWidgetKinds.PropertyList, Size = DashboardWidgetSizes.Full,
                Title = "Alle Treffer",
                Query = new DashboardPropertyQuery { Sort = "newest", Limit = 12 },
                Options = new DashboardWidgetOptions
                {
                    Variant = "list",
                    Fields = ["foto", "titel", "ort", "preis", "wohnflaeche"]
                }
            }
        ]
    };

    private static DashboardDefinition BuildInitialDefinition(string request) => new()
    {
        Title = "Ihre persönliche Übersicht",
        Intro = $"Demo-Übersicht zu Ihrem Wunsch: „{Truncate(request, 120)}“ (Mock-Modus, ohne KI erstellt).",
        Detail = new DashboardDetailSpec
        {
            Sections = ["gallery", "facts", "description", "contact"],
            Fields = ["preis", "preis-pro-m2", "wohnflaeche", "grundflaeche", "zimmer", "baujahr", "ort", "anbieter"]
        },
        Widgets =
        [
            new DashboardWidget
            {
                Id = "w1", Kind = DashboardWidgetKinds.StatRow, Size = DashboardWidgetSizes.Full,
                Query = new DashboardPropertyQuery(),
                Options = new DashboardWidgetOptions
                {
                    Tiles = [StatRowWidgetKeys.Total, StatRowWidgetKeys.NewLast7Days, StatRowWidgetKeys.MedianPrice]
                }
            },
            new DashboardWidget
            {
                Id = "w2", Kind = DashboardWidgetKinds.PropertyList, Size = DashboardWidgetSizes.L,
                Title = "Neueste Angebote",
                Query = new DashboardPropertyQuery { Sort = "newest", Limit = 6 },
                // Feldauswahl statt Preset - so sieht jeder lokale E2E-Lauf den fields-Pfad
                Options = new DashboardWidgetOptions
                {
                    Variant = "list",
                    Fields = ["foto", "titel", "ort", "preis", "wohnflaeche"]
                }
            },
            new DashboardWidget
            {
                Id = "w3", Kind = DashboardWidgetKinds.Map, Size = DashboardWidgetSizes.M,
                Title = "Alle Treffer auf der Karte",
                Query = new DashboardPropertyQuery()
            },
            new DashboardWidget
            {
                Id = "w4", Kind = DashboardWidgetKinds.NewListings, Size = DashboardWidgetSizes.M,
                Title = "Neu diese Woche",
                Query = new DashboardPropertyQuery { Limit = 4 }
            },
            new DashboardWidget
            {
                Id = "w5", Kind = DashboardWidgetKinds.PriceChart, Size = DashboardWidgetSizes.M,
                Title = "Preisverteilung",
                Query = new DashboardPropertyQuery(),
                Options = new DashboardWidgetOptions { Chart = "priceHistogram" }
            },
            new DashboardWidget
            {
                Id = "w6", Kind = DashboardWidgetKinds.Highlight, Size = DashboardWidgetSizes.Full,
                Title = "Günstigstes Angebot",
                Query = new DashboardPropertyQuery { Sort = "price-asc" }
            },
            new DashboardWidget
            {
                Id = "w7", Kind = DashboardWidgetKinds.TextNote, Size = DashboardWidgetSizes.Full,
                Options = new DashboardWidgetOptions
                {
                    Text = "Diese Übersicht wurde im Mock-Modus ohne KI erstellt. " +
                           "Mit aktiviertem AiConnector richtet sich der Aufbau nach Ihrem Wunsch."
                }
            }
        ]
    };

    private static DashboardDefinition BuildRefinedDefinition(DashboardDefinition current, string instruction, string viewType)
    {
        // Listen-Ansicht erlaubt nur das eine Listen-Widget - dort wird die
        // Anweisung im Intro vermerkt statt als Notiz-Widget angehaengt
        if (viewType == DashboardViewTypes.List)
        {
            current.Intro = $"Mock-Modus: Anweisung „{Truncate(instruction, 160)}“ vermerkt - " +
                            "mit aktiviertem AiConnector wird die Liste wirklich umgebaut.";
            return current;
        }

        current.Widgets.Add(new DashboardWidget
        {
            Id = $"w{current.Widgets.Count + 1}",
            Kind = DashboardWidgetKinds.TextNote,
            Size = DashboardWidgetSizes.Full,
            Title = "Ihr Wunsch",
            Options = new DashboardWidgetOptions
            {
                Text = $"Mock-Modus: Die Anweisung „{Truncate(instruction, 200)}“ wurde als Notiz angehängt. " +
                       "Mit aktiviertem AiConnector wird die Übersicht wirklich umgebaut."
            }
        });
        return current;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";

    /// <summary>Tile-Schluessel lokal gespiegelt, um keine Resolver-Abhaengigkeit aufzubauen</summary>
    private static class StatRowWidgetKeys
    {
        public const string Total = "total";
        public const string NewLast7Days = "newLast7Days";
        public const string MedianPrice = "medianPrice";
    }
}
