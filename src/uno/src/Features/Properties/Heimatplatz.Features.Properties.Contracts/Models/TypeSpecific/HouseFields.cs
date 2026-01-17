namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Bindable fields for house-specific data
/// </summary>
public class HouseFields
{
    public string? NumberOfFloors { get; set; }
    public bool HasBasement { get; set; }
    public bool HasGarden { get; set; }
    public string? GardenArea { get; set; }
    public bool HasGarage { get; set; }
    public string? GarageType { get; set; }
    public string? YearBuilt { get; set; }
    public string? Condition { get; set; }
}
