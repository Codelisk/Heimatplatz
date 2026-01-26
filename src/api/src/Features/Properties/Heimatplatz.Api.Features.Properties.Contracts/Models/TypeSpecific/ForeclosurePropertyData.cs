using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;

namespace Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific data for foreclosure/auction properties
/// </summary>
/// <param name="CourtName">Name of the court handling the foreclosure</param>
/// <param name="AuctionDate">Date and time of the auction</param>
/// <param name="MinimumBid">Minimum bid amount for the auction</param>
/// <param name="EstimatedValue">Estimated market value (Schaetzwert)</param>
/// <param name="Encumbrances">List of encumbrances (debts/liens) on the property</param>
/// <param name="Status">Current legal status of the foreclosure</param>
/// <param name="FileNumber">Court file/case number (Aktenzeichen)</param>
/// <param name="RegistrationNumber">Land register entry number (Einlagezahl EZ)</param>
/// <param name="CadastralMunicipality">Cadastral municipality (Katastralgemeinde KG)</param>
/// <param name="PlotNumber">Plot/parcel number (Grundstuecksnummer)</param>
/// <param name="TotalArea">Total area in square meters</param>
/// <param name="BuildingArea">Building area in square meters</param>
/// <param name="ZoningDesignation">Zoning type (Flaechenwidmung)</param>
/// <param name="BuildingCondition">Building condition description</param>
/// <param name="NumberOfRooms">Number of rooms (if building exists)</param>
/// <param name="YearBuilt">Year of construction (if building exists)</param>
/// <param name="ViewingDate">Scheduled viewing/inspection date</param>
/// <param name="BiddingDeadline">Deadline for submitting bids</param>
/// <param name="OwnershipShare">Ownership share being auctioned</param>
/// <param name="Notes">Additional notes and information</param>
/// <param name="EdictUrl">URL to the official edict document</param>
/// <param name="FloorPlanUrl">URL to floor plan document</param>
/// <param name="SitePlanUrl">URL to site plan document</param>
/// <param name="LongAppraisalUrl">URL to detailed appraisal</param>
/// <param name="ShortAppraisalUrl">URL to summary appraisal</param>
public record ForeclosurePropertyData(
    string CourtName,
    DateTime AuctionDate,
    decimal MinimumBid,
    decimal? EstimatedValue,
    List<Encumbrance> Encumbrances,
    LegalStatus Status,
    string FileNumber,
    string? RegistrationNumber = null,
    string? CadastralMunicipality = null,
    string? PlotNumber = null,
    decimal? TotalArea = null,
    decimal? BuildingArea = null,
    string? ZoningDesignation = null,
    string? BuildingCondition = null,
    int? NumberOfRooms = null,
    int? YearBuilt = null,
    DateTime? ViewingDate = null,
    DateTime? BiddingDeadline = null,
    string? OwnershipShare = null,
    string? Notes = null,
    string? EdictUrl = null,
    string? FloorPlanUrl = null,
    string? SitePlanUrl = null,
    string? LongAppraisalUrl = null,
    string? ShortAppraisalUrl = null
);
