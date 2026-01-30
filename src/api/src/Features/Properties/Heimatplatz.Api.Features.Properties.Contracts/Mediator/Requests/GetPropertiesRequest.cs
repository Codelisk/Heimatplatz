using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve a filtered and paginated list of properties.
/// All filtering is done server-side.
/// </summary>
public record GetPropertiesRequest(
    // Pagination
    int Page = 0,
    int PageSize = 20,

    // Filter: PropertyType (Multi-Select as JSON, e.g. "[0,1,2]")
    string? PropertyTypesJson = null,

    // Filter: SellerType (Multi-Select as JSON, e.g. "[0,1,2]")
    string? SellerTypesJson = null,

    // Filter: Municipalities (Multi-Select as JSON with GUIDs, e.g. "[\"guid1\",\"guid2\"]")
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
    string? ExcludedSellerSourceIdsJson = null
) : IRequest<GetPropertiesResponse>
{
    /// <summary>
    /// Parsed PropertyTypes from JSON string (e.g. "[0,1,2]")
    /// </summary>
    public List<PropertyType> GetPropertyTypes()
    {
        if (string.IsNullOrEmpty(PropertyTypesJson))
            return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<PropertyType>>(PropertyTypesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Parsed SellerTypes from JSON string (e.g. "[0,1,2]")
    /// </summary>
    public List<SellerType> GetSellerTypes()
    {
        if (string.IsNullOrEmpty(SellerTypesJson))
            return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<SellerType>>(SellerTypesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Parsed MunicipalityIds from JSON string (e.g. "[\"guid1\",\"guid2\"]")
    /// </summary>
    public List<Guid> GetMunicipalityIds()
    {
        if (string.IsNullOrEmpty(MunicipalityIdsJson))
            return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(MunicipalityIdsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Parsed ExcludedSellerSourceIds from JSON string (e.g. "[\"guid1\",\"guid2\"]")
    /// </summary>
    public List<Guid> GetExcludedSellerSourceIds()
    {
        if (string.IsNullOrEmpty(ExcludedSellerSourceIdsJson))
            return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(ExcludedSellerSourceIdsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

/// <summary>
/// Response with paginated property list
/// </summary>
public record GetPropertiesResponse(
    List<PropertyListItemDto> Properties,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);
