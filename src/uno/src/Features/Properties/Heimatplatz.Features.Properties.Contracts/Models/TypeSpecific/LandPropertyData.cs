using Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific.Enums;

namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific data for land/plot properties
/// </summary>
/// <param name="PlotSizeInSquareMeters">Size of the plot in square meters</param>
/// <param name="Zoning">Zoning type of the land</param>
/// <param name="HasBuildingRights">Whether building rights are granted</param>
/// <param name="SoilQuality">Quality of the soil</param>
/// <param name="IsBuildable">Whether the plot is immediately buildable</param>
/// <param name="HasUtilities">Whether utilities (water, electricity, etc.) are available</param>
public record LandPropertyData(
    decimal PlotSizeInSquareMeters,
    ZoningType Zoning,
    bool HasBuildingRights,
    SoilQuality? SoilQuality,
    bool IsBuildable = false,
    bool HasUtilities = false
);
