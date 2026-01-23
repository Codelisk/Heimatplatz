namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO fuer die Filtereinstellungen eines Benutzers.
/// Enthaelt: SelectedOrte (Liste der ausgewaehlten Orte),
/// SelectedAgeFilter (Zeitfilter),
/// IsHausSelected, IsGrundstueckSelected, IsZwangsversteigerungSelected (Immobilientypen)
/// </summary>
public record FilterPreferencesDto(
    List<string> SelectedOrte,
    AgeFilter SelectedAgeFilter,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected
)
{
    /// <summary>
    /// Erstellt leere Standard-Filtereinstellungen (alle 3 Typen selektiert)
    /// </summary>
    public static FilterPreferencesDto Default => new(
        SelectedOrte: [],
        SelectedAgeFilter: AgeFilter.Alle,
        IsHausSelected: true,
        IsGrundstueckSelected: true,
        IsZwangsversteigerungSelected: true
    );
}
