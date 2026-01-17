namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Encumbrance (debt/lien) on a foreclosure property
/// </summary>
/// <param name="Description">Description of the encumbrance</param>
/// <param name="Amount">Amount of the encumbrance in currency</param>
/// <param name="Creditor">Name of the creditor</param>
public record Encumbrance(
    string Description,
    decimal Amount,
    string Creditor
);
