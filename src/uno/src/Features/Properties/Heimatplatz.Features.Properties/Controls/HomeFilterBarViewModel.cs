using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// ViewModel für die HomeFilterBar im AppHeader.
/// Synchronisiert Filter-State mit dem zentralen FilterStateService.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class HomeFilterBarViewModel : ObservableObject
{
    private readonly IFilterStateService _filterStateService;

    [ObservableProperty]
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _isHausSelected;

    [ObservableProperty]
    private bool _isGrundstueckSelected;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected;

    [ObservableProperty]
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;

    [ObservableProperty]
    private List<string> _selectedOrte = [];

    [ObservableProperty]
    private int _resultCount;

    /// <summary>
    /// Hierarchische Bezirk/Ort-Struktur für OÖ
    /// </summary>
    public List<BezirkModel> Bezirke { get; } =
    [
        new("Linz-Land", "Traun", "Leonding", "Ansfelden", "Pasching", "Hörsching"),
        new("Linz-Stadt", "Linz", "Urfahr"),
        new("Wels-Land", "Wels", "Marchtrenk", "Gunskirchen"),
        new("Steyr-Land", "Steyr", "Sierning", "Garsten"),
    ];

    public HomeFilterBarViewModel(IFilterStateService filterStateService)
    {
        _filterStateService = filterStateService;

        // Subscribe to filter state changes
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        // Initialize from current state
        SyncFromFilterState();
    }

    private void OnFilterStateChanged(object? sender, EventArgs e)
    {
        SyncFromFilterState();
    }

    private bool _isSyncing;

    private void SyncFromFilterState()
    {
        _isSyncing = true;
        try
        {
            var state = _filterStateService.CurrentState;
            IsAllSelected = state.IsAllSelected;
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
            SelectedAgeFilter = state.SelectedAgeFilter;
            SelectedOrte = state.SelectedOrte.ToList();
            ResultCount = state.ResultCount;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (value)
        {
            IsHausSelected = false;
            IsGrundstueckSelected = false;
            IsZwangsversteigerungSelected = false;
        }
        UpdateFilterState();
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (value) IsAllSelected = false;
        else if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            IsAllSelected = true;

        UpdateFilterState();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (value) IsAllSelected = false;
        else if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            IsAllSelected = true;

        UpdateFilterState();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (value) IsAllSelected = false;
        else if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            IsAllSelected = true;

        UpdateFilterState();
    }

    partial void OnSelectedAgeFilterChanged(AgeFilter value)
    {
        if (_isSyncing) return;
        UpdateFilterState();
    }

    partial void OnSelectedOrteChanged(List<string> value)
    {
        if (_isSyncing) return;
        UpdateFilterState();
    }

    private void UpdateFilterState()
    {
        _filterStateService.UpdateFilters(
            IsAllSelected,
            IsHausSelected,
            IsGrundstueckSelected,
            IsZwangsversteigerungSelected,
            SelectedAgeFilter,
            SelectedOrte);
    }
}
