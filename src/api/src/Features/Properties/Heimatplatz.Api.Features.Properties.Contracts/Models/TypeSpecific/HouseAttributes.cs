namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific attributes for houses
/// </summary>
public class HouseAttributes
{
    /// <summary>Number of floors/stories</summary>
    public int? NumberOfFloors { get; set; }

    /// <summary>Basement available</summary>
    public bool HasBasement { get; set; }

    /// <summary>Garden available</summary>
    public bool HasGarden { get; set; }

    /// <summary>Garden area in m²</summary>
    public decimal? GardenArea { get; set; }

    /// <summary>Garage available</summary>
    public bool HasGarage { get; set; }

    /// <summary>Type of garage</summary>
    public GarageType? GarageType { get; set; }

    /// <summary>Year of construction</summary>
    public int? YearBuilt { get; set; }

    /// <summary>Property condition</summary>
    public PropertyCondition? Condition { get; set; }
}
