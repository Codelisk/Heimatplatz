using Heimatplatz.Maui.Features.Properties.Models;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Zentraler Service fuer Filter-State Management (In-Memory, Session).
/// Synchronisiert Filter-State zwischen FilterPreferencesPage und HomePage.
/// </summary>
[Singleton]
public class FilterStateService : IFilterStateService
{
    private FilterState _currentState = new();

    public FilterState CurrentState => _currentState;

    /// <summary>
    /// True nachdem UpdateFilters mindestens einmal aufgerufen wurde (Session-State existiert).
    /// </summary>
    public bool HasSessionState { get; private set; }

    public event EventHandler? FilterStateChanged;
    public event EventHandler? ResultCountChanged;

    public void UpdateFilters(
        bool isHausSelected,
        bool isGrundstueckSelected,
        bool isZwangsversteigerungSelected,
        AgeFilter selectedAgeFilter,
        List<string> selectedOrte,
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        List<Guid>? excludedSellerSourceIds = null,
        SortOption selectedSort = SortOption.Neueste)
    {
        HasSessionState = true;
        _currentState = _currentState with
        {
            IsHausSelected = isHausSelected,
            IsGrundstueckSelected = isGrundstueckSelected,
            IsZwangsversteigerungSelected = isZwangsversteigerungSelected,
            SelectedAgeFilter = selectedAgeFilter,
            SelectedOrte = selectedOrte,
            IsPrivateSelected = isPrivateSelected,
            IsBrokerSelected = isBrokerSelected,
            ExcludedSellerSourceIds = excludedSellerSourceIds ?? [],
            SelectedSort = selectedSort
        };

        FilterStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetResultCount(int count)
    {
        if (_currentState.ResultCount != count)
        {
            _currentState = _currentState with { ResultCount = count };
            ResultCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
