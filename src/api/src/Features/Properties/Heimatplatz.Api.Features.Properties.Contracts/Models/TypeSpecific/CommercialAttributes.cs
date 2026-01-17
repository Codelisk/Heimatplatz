namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific attributes for commercial properties
/// </summary>
public class CommercialAttributes
{
    /// <summary>Type of commercial use</summary>
    public CommercialUseType? UseType { get; set; }

    /// <summary>Total commercial area in m²</summary>
    public decimal? CommercialArea { get; set; }

    /// <summary>Loading dock available</summary>
    public bool HasLoadingDock { get; set; }

    /// <summary>Office area in m²</summary>
    public decimal? OfficeArea { get; set; }

    /// <summary>Storage/warehouse area in m²</summary>
    public decimal? StorageArea { get; set; }

    /// <summary>Number of parking spaces</summary>
    public int? ParkingSpaces { get; set; }
}
