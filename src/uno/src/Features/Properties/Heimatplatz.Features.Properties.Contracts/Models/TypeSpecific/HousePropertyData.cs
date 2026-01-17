using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific data for house/apartment properties
/// </summary>
/// <param name="LivingAreaInSquareMeters">Living area in square meters</param>
/// <param name="TotalRooms">Total number of rooms</param>
/// <param name="Bedrooms">Number of bedrooms</param>
/// <param name="Bathrooms">Number of bathrooms</param>
/// <param name="YearBuilt">Year the property was built</param>
/// <param name="Floors">Number of floors in the building</param>
/// <param name="Condition">Condition of the property</param>
/// <param name="HasGarage">Whether a garage is available</param>
/// <param name="HasGarden">Whether a garden is available</param>
/// <param name="ApartmentFloor">Floor number (for apartments only)</param>
/// <param name="HasElevator">Whether an elevator is available (for apartments)</param>
/// <param name="HasBasement">Whether a basement is available</param>
public record HousePropertyData(
    decimal LivingAreaInSquareMeters,
    int TotalRooms,
    int Bedrooms,
    int Bathrooms,
    int? YearBuilt,
    int Floors,
    PropertyCondition Condition,
    bool HasGarage = false,
    bool HasGarden = false,
    int? ApartmentFloor = null,
    bool? HasElevator = null,
    bool HasBasement = false
);
