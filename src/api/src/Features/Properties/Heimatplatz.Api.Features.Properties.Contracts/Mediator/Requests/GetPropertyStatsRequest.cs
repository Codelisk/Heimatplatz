using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Kennzahlen zu einer gefilterten Treffermenge (Dashboard-stat-row, spaeter auch
/// Suche). Nimmt dieselben Filter-Parameter wie GetPropertiesRequest, damit
/// Kennzahlen und Trefferliste garantiert dieselbe Menge meinen
/// (PropertyQueryFilters.ApplyCommonFilters).
/// </summary>
public record GetPropertyStatsRequest(
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
) : IRequest<GetPropertyStatsResponse>
{
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
/// Kennzahlen der Treffermenge. Preiswerte sind null, wenn keine Treffer existieren.
/// </summary>
/// <param name="Total">Alle Treffer der Filterung</param>
/// <param name="NewLast7Days">Davon in den letzten 7 Tagen eingestellt</param>
/// <param name="MedianPrice">Median (bei gerader Anzahl der untere der beiden mittleren Werte)</param>
public record GetPropertyStatsResponse(
    int Total,
    int NewLast7Days,
    decimal? MinPrice,
    decimal? MedianPrice,
    decimal? MaxPrice
);
