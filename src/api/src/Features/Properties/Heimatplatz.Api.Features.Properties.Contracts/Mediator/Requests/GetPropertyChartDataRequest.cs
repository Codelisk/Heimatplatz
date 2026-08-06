using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Rohwerte fuer server-gerenderte Diagramme zu einer gefilterten Treffermenge
/// (Dashboard-price-chart): Preise + Einstelldaten, gekappt auf die neuesten
/// <see cref="MaxItems"/> Treffer. Nimmt dieselben Filter-Parameter wie
/// GetPropertiesRequest (PropertyQueryFilters), damit Diagramm und Trefferliste
/// garantiert dieselbe Menge meinen.
/// </summary>
public record GetPropertyChartDataRequest(
    // Filter: PropertyType (Multi-Select as JSON, e.g. "[1,2]")
    string? PropertyTypesJson = null,

    // Filter: SellerType (Multi-Select as JSON)
    string? SellerTypesJson = null,

    // Filter: Municipalities (Multi-Select as JSON with GUIDs)
    string? MunicipalityIdsJson = null,

    // Filter: Age (CreatedAt >= DateTime)
    DateTime? CreatedAfter = null,

    // Filter: Price
    decimal? PriceMin = null,
    decimal? PriceMax = null,

    // Filter: Area
    int? AreaMin = null,
    int? AreaMax = null,

    // Filter: Rooms
    int? RoomsMin = null,

    // Filter: Excluded seller sources
    string? ExcludedSellerSourceIdsJson = null,

    // Filter: Volltextsuche (Titel, Beschreibung, Adresse, Gemeindename)
    string? SearchText = null,

    // Filter: Neubauprojekte (null/true = anzeigen, false = ausblenden; wie GetPropertiesRequest)
    bool? IncludeNewBuildProjects = null
) : IRequest<GetPropertyChartDataResponse>
{
    /// <summary>Obergrenze der gelieferten Wertepaare (neueste zuerst)</summary>
    public const int MaxItems = 2000;

    /// <summary>Parsed PropertyTypes from JSON string (tolerant: defekt = leer)</summary>
    public List<PropertyType> GetPropertyTypes() => ParseList<PropertyType>(PropertyTypesJson);

    /// <summary>Parsed SellerTypes from JSON string (tolerant: defekt = leer)</summary>
    public List<SellerType> GetSellerTypes() => ParseList<SellerType>(SellerTypesJson);

    /// <summary>Parsed MunicipalityIds from JSON string (tolerant: defekt = leer)</summary>
    public List<Guid> GetMunicipalityIds() => ParseList<Guid>(MunicipalityIdsJson);

    /// <summary>Parsed ExcludedSellerSourceIds from JSON string (tolerant: defekt = leer)</summary>
    public List<Guid> GetExcludedSellerSourceIds() => ParseList<Guid>(ExcludedSellerSourceIdsJson);

    private static List<T> ParseList<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

/// <summary>
/// Rohwerte der Treffermenge. Prices und CreatedAtUtc sind positionsgleich
/// (ein Eintrag pro Treffer, neueste zuerst).
/// </summary>
/// <param name="Total">Alle Treffer der Filterung (auch ueber die Kappung hinaus)</param>
/// <param name="Truncated">True wenn mehr Treffer existieren als geliefert wurden</param>
public record GetPropertyChartDataResponse(
    int Total,
    List<decimal> Prices,
    List<DateTime> CreatedAtUtc,
    bool Truncated
);
