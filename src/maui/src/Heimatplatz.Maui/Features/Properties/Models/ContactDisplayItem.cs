namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>Ein Kontakt der Immobilie fuer die Anzeige auf der Detailseite.</summary>
public record ContactDisplayItem(
    string Name,
    string? Email,
    string? Phone
)
{
    public bool HasName => !string.IsNullOrWhiteSpace(Name);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool HasPhone => !string.IsNullOrWhiteSpace(Phone);
}
