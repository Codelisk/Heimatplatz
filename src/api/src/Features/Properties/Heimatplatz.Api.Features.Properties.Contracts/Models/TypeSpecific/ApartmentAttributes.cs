namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific attributes for apartments
/// </summary>
public class ApartmentAttributes
{
    /// <summary>Floor number (0 = ground floor, negative = basement)</summary>
    public int? Floor { get; set; }

    /// <summary>Elevator available</summary>
    public bool HasElevator { get; set; }

    /// <summary>Balcony or terrace available</summary>
    public bool HasBalcony { get; set; }

    /// <summary>Balcony/terrace area in m²</summary>
    public decimal? BalconyArea { get; set; }

    /// <summary>Parking space available</summary>
    public bool HasParking { get; set; }

    /// <summary>Type of parking</summary>
    public ParkingType? ParkingType { get; set; }

    /// <summary>Year of construction</summary>
    public int? YearBuilt { get; set; }

    /// <summary>Property condition</summary>
    public PropertyCondition? Condition { get; set; }
}
