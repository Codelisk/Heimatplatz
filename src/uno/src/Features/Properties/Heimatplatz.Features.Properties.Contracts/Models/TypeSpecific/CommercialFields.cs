namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Bindable fields for commercial-specific data
/// </summary>
public class CommercialFields
{
    public string? UseType { get; set; }
    public string? CommercialArea { get; set; }
    public bool HasLoadingDock { get; set; }
    public string? OfficeArea { get; set; }
    public string? StorageArea { get; set; }
    public string? ParkingSpaces { get; set; }
}
