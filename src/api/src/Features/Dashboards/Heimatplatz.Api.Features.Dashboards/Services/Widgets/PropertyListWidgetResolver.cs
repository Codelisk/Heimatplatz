using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Trefferliste (property-list): das Brot-und-Butter-Widget jeder Uebersicht.
/// Darstellungsvarianten grid/list/minimal steuern die Informationsdichte
/// ("nur die Daten, die der Nutzer sich wuenscht").
/// </summary>
public class PropertyListWidgetResolver(
    IMediator mediator,
    IOptions<DashboardOptions> options
) : IDashboardWidgetResolver
{
    public const int DefaultLimit = 6;

    private static readonly string[] AllowedVariants = ["grid", "list", "minimal"];

    public string Kind => DashboardWidgetKinds.PropertyList;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Trefferliste passender Inserate.",
        $"query wird unterstuetzt (limit 1-{options.Value.Limits.MaxListItems}, Default {DefaultLimit}). " +
        "options.variant: \"grid\" (grosse Foto-Karten), \"list\" (kompakte Zeilen MIT kleinem Foto), " +
        "\"minimal\" (OHNE Fotos: Titel, Ort, Preis, Flaeche - fuer Wuensche wie \"ohne Bilder\"). " +
        "options.fields: geordnete Feld-Schluessel aus dem FELD-KATALOG - nutzen, wenn der Kunde " +
        "sagt, WELCHE Werte er sehen will (uebersteuert das Preset).");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        widget.Query = PropertyQueryMapper.Sanitize(widget.Query, options.Value.Limits.MaxListItems, warnings);
        widget.Query.Limit ??= DefaultLimit;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.L);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);

        var variant = widget.Options?.Variant?.Trim().ToLowerInvariant();
        widget.Options = new DashboardWidgetOptions
        {
            Variant = variant is not null && AllowedVariants.Contains(variant) ? variant : "grid",
            Fields = DashboardFieldCatalog.NormalizeFields(widget.Options?.Fields, forDetail: false, warnings)
        };

        return widget;
    }

    public async Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken)
    {
        var query = widget.Query ?? new DashboardPropertyQuery();
        var request = PropertyQueryMapper.ToGetPropertiesRequest(query, query.Limit ?? DefaultLimit);
        var result = await mediator.Request(request, cancellationToken);

        return new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            PropertyList: new PropertyListWidgetData(result.Result.Properties, result.Result.Total));
    }
}
