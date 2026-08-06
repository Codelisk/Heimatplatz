using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Faltkarte (map) mit den Pins der Treffermenge. Privacy-Jitter fuer ungenaue
/// Lagen passiert serverseitig im MapPins-Handler - exakte Koordinaten verlassen
/// den Server nie ungewollt.
/// </summary>
public class MapWidgetResolver(
    IMediator mediator,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public string Kind => DashboardWidgetKinds.Map;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Karte mit den Treffern als Pins.",
        "query wird unterstuetzt (limit/sort wirkungslos). Nur sinnvoll bei Ortsbezug des Wunschs.");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit = null;
        widget.Query.Sort = null;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.M);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);
        widget.Options = null;

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var result = await mediator.Request(PropertyQueryMapper.ToGetPropertyMapPinsRequest(query), cancellationToken);
        var pins = result.Result;

        return new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            Map: new MapWidgetData(pins.Pins, pins.Total, pins.WithoutCoordinates, pins.Truncated));
    }
}
