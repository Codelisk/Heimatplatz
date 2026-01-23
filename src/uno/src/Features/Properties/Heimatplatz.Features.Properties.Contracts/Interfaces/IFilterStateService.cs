using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service für zentrales Filter-State Management.
/// Synchronisiert Filter-State zwischen HomeFilterBar (Header) und HomePage.
/// </summary>
public interface IFilterStateService
{
    /// <summary>
    /// Aktueller Filter-State
    /// </summary>
    FilterState CurrentState { get; }

    /// <summary>
    /// Event wenn sich der Filter-State ändert
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
        List<string> selectedOrte);

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
    public int ResultCount { get; init; }
}
