using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service fuer zentrales Filter-State Management (In-Memory Session-Filter).
/// Haelt den Filter-State der Filterleiste auf der HomePage fuer die Session.
/// </summary>
public interface IFilterStateService
{
    /// <summary>
    /// Aktueller Filter-State
    /// </summary>
    FilterState CurrentState { get; }

    /// <summary>
    /// True nachdem UpdateFilters mindestens einmal aufgerufen wurde (Session-State existiert).
    /// </summary>
    bool HasSessionState { get; }

    /// <summary>
    /// Event wenn sich der Filter-State aendert
    /// </summary>
    event EventHandler? FilterStateChanged;

    /// <summary>
    /// Aktualisiert die Filter-Einstellungen
    /// </summary>
    void UpdateFilters(
        bool isHausSelected,
        bool isGrundstueckSelected,
        bool isZwangsversteigerungSelected,
        AgeFilter selectedAgeFilter,
        List<string> selectedOrte,
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        List<Guid>? excludedSellerSourceIds = null,
        SortOption selectedSort = SortOption.Neueste);

    /// <summary>
    /// Event wenn sich die Ergebnis-Anzahl aendert (separat von FilterStateChanged
    /// um rekursive Schleifen zu vermeiden)
    /// </summary>
    event EventHandler? ResultCountChanged;

    /// <summary>
    /// Setzt die Ergebnis-Anzahl
    /// </summary>
    void SetResultCount(int count);
}

/// <summary>
/// Aktueller Filter-State
/// </summary>
public record FilterState
{
    public bool IsHausSelected { get; init; } = true;
    public bool IsGrundstueckSelected { get; init; } = true;
    public bool IsZwangsversteigerungSelected { get; init; } = true;
    public AgeFilter SelectedAgeFilter { get; init; } = AgeFilter.Alle;
    public IReadOnlyList<string> SelectedOrte { get; init; } = [];
    public bool IsPrivateSelected { get; init; } = true;
    public bool IsBrokerSelected { get; init; } = true;
    public IReadOnlyList<Guid> ExcludedSellerSourceIds { get; init; } = [];
    public SortOption SelectedSort { get; init; } = SortOption.Neueste;
    public int ResultCount { get; init; }
}
