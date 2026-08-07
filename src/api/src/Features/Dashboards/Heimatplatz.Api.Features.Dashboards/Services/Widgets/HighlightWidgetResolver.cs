using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Hervorgehobenes Einzel-Inserat (highlight): der Top-Treffer der Query als
/// Hero-Karte, z.B. "Guenstigstes Angebot" (sort price-asc) oder "Neuestes" (newest).
/// </summary>
public class HighlightWidgetResolver(
    IMediator mediator,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public string Kind => DashboardWidgetKinds.Highlight;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "EIN hervorgehobenes Top-Inserat (der erste Treffer der Sortierung).",
        "query wird unterstuetzt (limit ist immer 1; sort bestimmt, WAS hervorgehoben wird, " +
        "z.B. price-asc = guenstigstes, newest = neuestes). options.fields wie bei property-list " +
        "(steuert die Info-Zeile).");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit = 1;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.Full);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);

        var fields = DashboardFieldCatalog.NormalizeFields(widget.Options?.Fields, forDetail: false, warnings);
        widget.Options = fields is null ? null : new DashboardWidgetOptions { Fields = fields };

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, WidgetResolveContext context, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var request = PropertyQueryMapper.ToGetPropertiesRequest(query, pageSize: 1);
        var result = await mediator.Request(request, cancellationToken);

        return new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            PropertyList: new PropertyListWidgetData(result.Result.Properties, result.Result.Total));
    }
}
