using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer die Kartenansicht: liefert leichte Pin-DTOs aller Treffer mit
/// Koordinaten. Nimmt dieselben Filter-Parameter wie GetPropertiesRequest
/// (der Web-Client baut beide Query-Strings aus demselben Zustand), aber ohne
/// Paging/Sortierung - die Karte zeigt alle Treffer bis zum Pin-Deckel.
/// </summary>
public record GetPropertyMapPinsRequest(
    // Filter: PropertyType (Multi-Select as JSON, e.g. "[\"House\",\"Land\"]")
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
    string? SearchText = null
) : IRequest<GetPropertyMapPinsResponse>
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
/// Antwort der Kartenansicht.
/// </summary>
/// <param name="Pins">Treffer mit Koordinaten (bis zum Pin-Deckel, neueste zuerst)</param>
/// <param name="Total">Alle Treffer der Filterung (mit und ohne Koordinaten)</param>
/// <param name="WithoutCoordinates">Treffer ohne Koordinaten - erscheinen nicht auf der Karte</param>
/// <param name="Truncated">True wenn mehr Treffer mit Koordinaten existieren als Pins geliefert wurden</param>
public record GetPropertyMapPinsResponse(
    List<PropertyMapPinDto> Pins,
    int Total,
    int WithoutCoordinates,
    bool Truncated
);

/// <summary>
/// Leichter Karten-Pin. IsApproximate=true bedeutet: die Lage ist bewusst ungenau
/// (Ortszentrum-Fallback plus serverseitige Streuung - Privatsphaere der Anbieter).
/// </summary>
public record PropertyMapPinDto(
    Guid Id,
    double Latitude,
    double Longitude,
    bool IsApproximate,
    PropertyType Type,
    SellerType SellerType,
    decimal Price,
    string Title,
    string City,
    string PostalCode,
    Guid MunicipalityId,
    string? ImageUrl,
    DateTime? AuctionDate
);
