namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Typspezifische Daten fuer Haus-/Wohnungs-Immobilien (deserialisiert aus PropertyDto.TypeSpecificData)
/// </summary>
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

/// <summary>
/// Typspezifische Daten fuer Grundstuecke (deserialisiert aus PropertyDto.TypeSpecificData)
/// </summary>
public record LandPropertyData(
    decimal PlotSizeInSquareMeters,
    ZoningType Zoning,
    bool HasBuildingRights,
    SoilQuality? SoilQuality,
    bool IsBuildable = false,
    bool HasUtilities = false
);

/// <summary>
/// Belastung (Schuld/Pfandrecht) einer Zwangsversteigerungs-Immobilie
/// </summary>
public record Encumbrance(
    string Description,
    decimal Amount,
    string Creditor
);

/// <summary>
/// Typspezifische Daten fuer Zwangsversteigerungen (deserialisiert aus PropertyDto.TypeSpecificData).
/// Teilmenge des API-Contracts; optionale Felder mit Default, damit aelterer Bestand ohne
/// diese Properties weiterhin deserialisiert.
/// </summary>
public record ForeclosurePropertyData(
    string CourtName,
    DateTime AuctionDate,
    decimal MinimumBid,
    List<Encumbrance> Encumbrances,
    LegalStatus Status,
    string FileNumber,
    decimal? EstimatedValue = null,
    decimal? TotalArea = null,
    decimal? BuildingArea = null
);
