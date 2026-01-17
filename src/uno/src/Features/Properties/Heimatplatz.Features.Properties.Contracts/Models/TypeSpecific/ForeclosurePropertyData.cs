using Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific.Enums;

namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Type-specific data for foreclosure/auction properties
/// </summary>
/// <param name="CourtName">Name of the court handling the foreclosure</param>
/// <param name="AuctionDate">Date and time of the auction</param>
/// <param name="MinimumBid">Minimum bid amount for the auction</param>
/// <param name="Encumbrances">List of encumbrances (debts/liens) on the property</param>
/// <param name="Status">Current legal status of the foreclosure</param>
/// <param name="FileNumber">Court file/case number</param>
public record ForeclosurePropertyData(
    string CourtName,
    DateTime AuctionDate,
    decimal MinimumBid,
    List<Encumbrance> Encumbrances,
    LegalStatus Status,
    string FileNumber
);
