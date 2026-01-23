using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Zentraler Service für Filter-State Management.
/// Synchronisiert Filter-State zwischen HomeFilterBar (Header) und HomePage.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class FilterStateService : IFilterStateService
{
    private FilterState _currentState = new();

    public FilterState CurrentState => _currentState;

    public event EventHandler? FilterStateChanged;

    public void UpdateFilters(
        bool isAllSelected,
        bool isHausSelected,
        bool isGrundstueckSelected,
        bool isZwangsversteigerungSelected,
        AgeFilter selectedAgeFilter,
        List<string> selectedOrte)
    {
        _currentState = _currentState with
        {
            IsAllSelected = isAllSelected,
            IsHausSelected = isHausSelected,
            IsGrundstueckSelected = isGrundstueckSelected,
            IsZwangsversteigerungSelected = isZwangsversteigerungSelected,
            SelectedAgeFilter = selectedAgeFilter,
            SelectedOrte = selectedOrte
        };

        FilterStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetResultCount(int count)
    {
        if (_currentState.ResultCount != count)
        {
            _currentState = _currentState with { ResultCount = count };
            FilterStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
