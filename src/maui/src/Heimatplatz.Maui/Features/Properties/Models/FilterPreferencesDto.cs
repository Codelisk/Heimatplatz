namespace Heimatplatz.Maui.Features.Properties.Models;

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
    bool IsZwangsversteigerungSelected,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    List<Guid> ExcludedSellerSourceIds,
    SortOption SelectedSort = SortOption.Neueste
)
{
    /// <summary>
    /// Erstellt leere Standard-Filtereinstellungen (Haus + Grundstueck selektiert,
    /// Zwangsversteigerungen standardmaessig deaktiviert - wie Web/API)
    /// </summary>
    public static FilterPreferencesDto Default => new(
        SelectedOrte: [],
        SelectedAgeFilter: AgeFilter.Alle,
        IsHausSelected: true,
        IsGrundstueckSelected: true,
        IsZwangsversteigerungSelected: false,
        IsPrivateSelected: true,
        IsBrokerSelected: true,
        ExcludedSellerSourceIds: [],
        SelectedSort: SortOption.Neueste
    );
}
