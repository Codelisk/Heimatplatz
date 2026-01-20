namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO fuer die Filtereinstellungen eines Benutzers.
/// Enthaelt: SelectedOrte (Liste der ausgewaehlten Orte),
/// SelectedAgeFilter (Zeitfilter), IsAllSelected (alle Typen),
/// IsHausSelected, IsGrundstueckSelected, IsZwangsversteigerungSelected (Immobilientypen)
/// </summary>
public record FilterPreferencesDto(
    List<string> SelectedOrte,
    AgeFilter SelectedAgeFilter,
    bool IsAllSelected,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected
)
{
    /// <summary>
    /// Erstellt leere Standard-Filtereinstellungen
    /// </summary>
    public static FilterPreferencesDto Default => new(
        SelectedOrte: [],
        SelectedAgeFilter: AgeFilter.Alle,
        IsAllSelected: true,
        IsHausSelected: false,
        IsGrundstueckSelected: false,
        IsZwangsversteigerungSelected: false
    );
}
