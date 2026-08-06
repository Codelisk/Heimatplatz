using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// "Neu diese Woche"-Feed (new-listings): die juengsten Zugaenge der Treffermenge
/// (festes 7-Tage-Fenster, neueste zuerst).
/// </summary>
public class NewListingsWidgetResolver(
    IMediator mediator,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public const int DefaultLimit = 6;
    public const int WindowDays = 7;

    public string Kind => DashboardWidgetKinds.NewListings;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Feed der Neuzugaenge der letzten 7 Tage (neueste zuerst).",
        $"query wird unterstuetzt (limit 1-{options.Value.Limits.MaxListItems}, Default {DefaultLimit}; " +
        "sort ist fest newest, das Zeitfenster fest 7 Tage). options.fields wie bei property-list.");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit ??= DefaultLimit;
        widget.Query.Sort = "newest";
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.M);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);

        var fields = DashboardFieldCatalog.NormalizeFields(widget.Options?.Fields, forDetail: false, warnings);
        widget.Options = fields is null ? null : new DashboardWidgetOptions { Fields = fields };

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var request = PropertyQueryMapper.ToGetPropertiesRequest(
            query,
            pageSize: query.Limit ?? DefaultLimit,
            createdAfter: DateTime.UtcNow.AddDays(-WindowDays));
        var result = await mediator.Request(request, cancellationToken);

        return new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            PropertyList: new PropertyListWidgetData(result.Result.Properties, result.Result.Total));
    }
}
