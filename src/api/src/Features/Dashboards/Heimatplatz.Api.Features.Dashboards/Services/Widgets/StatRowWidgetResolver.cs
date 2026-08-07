using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Kennzahl-Kacheln (stat-row): Trefferzahl, Neuzugaenge, Preisspanne der
/// gefilterten Menge. Labels und Werte kommen anzeigefertig vom Server
/// (Backend-First, Preisformat-Kanon "€ 520.000").
/// </summary>
public class StatRowWidgetResolver(
    IMediator mediator,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public const string TileTotal = "total";
    public const string TileNewLast7Days = "newLast7Days";
    public const string TileMinPrice = "minPrice";
    public const string TileMedianPrice = "medianPrice";
    public const string TileMaxPrice = "maxPrice";

    private static readonly string[] AllowedTiles =
        [TileTotal, TileNewLast7Days, TileMinPrice, TileMedianPrice, TileMaxPrice];

    private static readonly string[] DefaultTiles = [TileTotal, TileNewLast7Days, TileMedianPrice];

    public string Kind => DashboardWidgetKinds.StatRow;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Kennzahl-Kacheln zur gefilterten Treffermenge.",
        "query wird unterstuetzt (limit/sort wirkungslos). options.tiles: Auswahl aus " +
        "\"total\" (Trefferzahl), \"newLast7Days\" (neu in 7 Tagen), \"minPrice\", \"medianPrice\", \"maxPrice\" " +
        "(max. 5, Default total+newLast7Days+medianPrice).");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit = null;
        widget.Query.Sort = null;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.Full);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);

        var tiles = widget.Options?.Tiles?
            .Select(t => t?.Trim() ?? "")
            .Where(t => AllowedTiles.Contains(t, StringComparer.OrdinalIgnoreCase))
            .Select(t => AllowedTiles.First(a => string.Equals(a, t, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .Take(5)
            .ToList();

        widget.Options = new DashboardWidgetOptions
        {
            Tiles = tiles is { Count: > 0 } ? tiles : [.. DefaultTiles]
        };

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, WidgetResolveContext context, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var result = await mediator.Request(PropertyQueryMapper.ToGetPropertyStatsRequest(query), cancellationToken);
        var stats = result.Result;

        var tiles = (widget.Options?.Tiles is { Count: > 0 } selected ? selected : [.. DefaultTiles])
            .Select(key => BuildTile(key, stats))
            .ToList();

        return new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            StatRow: new StatRowWidgetData(tiles));
    }

    private static StatTileDto BuildTile(string key, GetPropertyStatsResponse stats) => key switch
    {
        TileTotal => new StatTileDto(key, "Treffer", PropertyQueryMapper.FormatCount(stats.Total)),
        TileNewLast7Days => new StatTileDto(key, "Neu in 7 Tagen", PropertyQueryMapper.FormatCount(stats.NewLast7Days)),
        TileMinPrice => new StatTileDto(key, "Günstigstes Angebot", FormatOptionalPrice(stats.MinPrice)),
        TileMedianPrice => new StatTileDto(key, "Mittlerer Preis", FormatOptionalPrice(stats.MedianPrice)),
        TileMaxPrice => new StatTileDto(key, "Teuerstes Angebot", FormatOptionalPrice(stats.MaxPrice)),
        _ => new StatTileDto(key, key, "–")
    };

    private static string FormatOptionalPrice(decimal? value) =>
        value.HasValue ? PropertyQueryMapper.FormatPrice(value.Value) : "–";
}
