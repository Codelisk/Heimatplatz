using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve a filtered list of properties
/// </summary>
public record GetPropertiesRequest(
    PropertyType? Type = null,
    string? SellerTypesJson = null,
    decimal? PriceMin = null,
    decimal? PriceMax = null,
    int? AreaMin = null,
    int? AreaMax = null,
    int? RoomsMin = null,
    string? City = null,
    string? ExcludedSellerSourceIdsJson = null,
    int Skip = 0,
    int Take = 20
) : IRequest<GetPropertiesResponse>
{
    /// <summary>
    /// Parsed SellerTypes from JSON string (e.g. "[1,2,3]")
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
/// Response with property list and total count
/// </summary>
public record GetPropertiesResponse(
    List<PropertyListItemDto> Properties,
    int Total
);
