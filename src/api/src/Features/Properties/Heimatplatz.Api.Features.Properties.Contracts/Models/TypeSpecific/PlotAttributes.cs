namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific attributes for plots/land
/// </summary>
public class PlotAttributes
{
    /// <summary>Plot is buildable</summary>
    public bool IsBuildable { get; set; }

    /// <summary>Buildable percentage (e.g., 40 = 40% of area can be built on)</summary>
    public decimal? BuildablePercentage { get; set; }

    /// <summary>Zoning type</summary>
    public ZoneType? ZoneType { get; set; }

    /// <summary>Plot has utilities (connections available)</summary>
    public bool HasUtilities { get; set; }

    /// <summary>Available utility types (Water, Electricity, Gas, Sewage)</summary>
    public List<string> UtilityTypes { get; set; } = [];

    /// <summary>Slope of the plot</summary>
    public SlopeType? SlopeType { get; set; }
}
