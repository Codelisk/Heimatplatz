namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Bindable fields for apartment-specific data
/// </summary>
public class ApartmentFields
{
    public string? Floor { get; set; }
    public bool HasElevator { get; set; }
    public bool HasBalcony { get; set; }
    public string? BalconyArea { get; set; }
    public bool HasParking { get; set; }
    public string? ParkingType { get; set; }
    public string? YearBuilt { get; set; }
    public string? Condition { get; set; }
}
