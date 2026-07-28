namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Eine Zeile der Lasten-Karte auf der ZV-Detailseite (Grundbuch-C-Blatt):
/// Bezeichnung, optional der Glaeubiger als Zweitzeile und der Betrag rechts.
/// </summary>
public record EncumbranceDisplayItem(
    string Description,
    string? Creditor,
    string? AmountText
)
{
    public bool HasCreditor => !string.IsNullOrWhiteSpace(Creditor);

    public bool HasAmount => !string.IsNullOrWhiteSpace(AmountText);
}
