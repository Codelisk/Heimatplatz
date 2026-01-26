using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// ViewModel für die HomeFilterBar im AppHeader.
/// Synchronisiert Filter-State mit dem zentralen FilterStateService.
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class HomeFilterBarViewModel : ObservableObject
{
    private readonly IFilterStateService _filterStateService;

    [ObservableProperty]
    private bool _isHausSelected = true;

    [ObservableProperty]
    private bool _isGrundstueckSelected = true;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected = true;

    [ObservableProperty]
    private bool _isPrivateSelected = true;

    [ObservableProperty]
    private bool _isBrokerSelected = true;

    [ObservableProperty]
    private bool _isPortalSelected = true;

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
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
            IsPrivateSelected = state.IsPrivateSelected;
            IsBrokerSelected = state.IsBrokerSelected;
            IsPortalSelected = state.IsPortalSelected;
            SelectedAgeFilter = state.SelectedAgeFilter;
            SelectedOrte = state.SelectedOrte.ToList();
            ResultCount = state.ResultCount;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsHausSelected = true;
            _isSyncing = false;
            return;
        }

        UpdateFilterState();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsGrundstueckSelected = true;
            _isSyncing = false;
            return;
        }

        UpdateFilterState();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsGrundstueckSelected)
        {
            _isSyncing = true;
            IsZwangsversteigerungSelected = true;
            _isSyncing = false;
            return;
        }

        UpdateFilterState();
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsBrokerSelected && !IsPortalSelected)
        {
            _isSyncing = true;
            IsPrivateSelected = true;
            _isSyncing = false;
            return;
        }

        UpdateFilterState();
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsPrivateSelected && !IsPortalSelected)
        {
            _isSyncing = true;
            IsBrokerSelected = true;
            _isSyncing = false;
            return;
        }

        UpdateFilterState();
    }

    partial void OnIsPortalSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsPrivateSelected && !IsBrokerSelected)
        {
            _isSyncing = true;
            IsPortalSelected = true;
            _isSyncing = false;
            return;
        }

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
            IsHausSelected,
            IsGrundstueckSelected,
            IsZwangsversteigerungSelected,
            SelectedAgeFilter,
            SelectedOrte,
            IsPrivateSelected,
            IsBrokerSelected,
            IsPortalSelected);
    }
}
