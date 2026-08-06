using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Services.Charts;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Server-gerendertes Diagramm (price-chart): Preisverteilung oder Neuzugaenge
/// pro Woche zur gefilterten Treffermenge. Die API malt das Bild (SkiaSharp,
/// Light+Dark als data:-URIs), die Frontends zeigen nur ein img - damit ist der
/// meistgewuenschte unsupportedWishes-Eintrag ("Diagramm") ohne Client-Charting
/// erfuellbar und MAUI bekommt es spaeter geschenkt.
/// </summary>
public class PriceChartWidgetResolver(
    IMediator mediator,
    DashboardChartRenderer renderer,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public const string ChartPriceHistogram = "priceHistogram";
    public const string ChartNewPerWeek = "newPerWeek";

    private static readonly string[] AllowedCharts = [ChartPriceHistogram, ChartNewPerWeek];

    public string Kind => DashboardWidgetKinds.PriceChart;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Diagramm zur Treffermenge (server-gerendertes Bild).",
        "query wird unterstuetzt (limit/sort wirkungslos). options.chart: " +
        "\"priceHistogram\" (Preisverteilung als Balken) | \"newPerWeek\" (Neuzugaenge der letzten 8 Wochen). " +
        "Fuer Wuensche nach Diagrammen, Verteilungen oder Markt-Ueberblick.");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit = null;
        widget.Query.Sort = null;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.M);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);

        var chart = widget.Options?.Chart?.Trim();
        widget.Options = new DashboardWidgetOptions
        {
            Chart = AllowedCharts.FirstOrDefault(a => string.Equals(a, chart, StringComparison.OrdinalIgnoreCase))
                ?? ChartPriceHistogram
        };

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var result = await mediator.Request(PropertyQueryMapper.ToGetPropertyChartDataRequest(query), cancellationToken);
        var data = result.Result;

        // Ohne Treffer gibt es nichts zu malen - der Renderer zeigt den Leer-Hinweis
        if (data.Prices.Count == 0)
            return new WidgetDataDto(widget.Id, Kind, Success: true, Error: null, Chart: null);

        var chartKind = widget.Options?.Chart ?? ChartPriceHistogram;
        ChartWidgetData chart = chartKind == ChartNewPerWeek
            ? new ChartWidgetData(
                chartKind,
                renderer.RenderNewPerWeekDataUri(data.CreatedAtUtc, dark: false),
                renderer.RenderNewPerWeekDataUri(data.CreatedAtUtc, dark: true),
                AltText: $"Balkendiagramm: neue Inserate pro Kalenderwoche der letzten {DashboardChartRenderer.WeeksWindow} Wochen.",
                Caption: BuildCaption(data.Total, data.Truncated))
            : new ChartWidgetData(
                chartKind,
                renderer.RenderPriceHistogramDataUri(data.Prices, dark: false),
                renderer.RenderPriceHistogramDataUri(data.Prices, dark: true),
                AltText: $"Preisverteilung von {DashboardChartRenderer.FormatEuroCompact(data.Prices.Min())} € bis " +
                         $"{DashboardChartRenderer.FormatEuroCompact(data.Prices.Max())} € über {data.Total} Inserate.",
                Caption: BuildCaption(data.Total, data.Truncated));

        return new WidgetDataDto(widget.Id, Kind, Success: true, Error: null, Chart: chart);
    }

    private static string BuildCaption(int total, bool truncated) =>
        truncated
            ? $"Datenbasis: die neuesten {PropertyQueryMapper.FormatCount(2000)} von {PropertyQueryMapper.FormatCount(total)} Treffern"
            : $"Datenbasis: {PropertyQueryMapper.FormatCount(total)} Treffer";
}
