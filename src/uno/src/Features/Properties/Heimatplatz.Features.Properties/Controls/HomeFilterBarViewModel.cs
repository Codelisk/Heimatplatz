using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// ViewModel fuer die HomeFilterBar im AppHeader.
/// Synchronisiert Filter-State mit dem zentralen FilterStateService.
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class HomeFilterBarViewModel : ObservableObject
{
    private readonly IFilterStateService _filterStateService;
    private readonly ILocationService _locationService;

    [ObservableProperty]
    private bool _isHausSelected = true;

    [ObservableProperty]
    private bool _isGrundstueckSelected = true;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected = true;

    [ObservableProperty]
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;

    /// <summary>
    /// Liste der Anbietertypen fuer den SellerTypePicker
    /// </summary>
    [ObservableProperty]
    private List<SellerTypeModel> _sellerTypes = SellerTypeModel.CreateDefaultList();

    [ObservableProperty]
    private List<string> _selectedOrte = [];

    [ObservableProperty]
    private int _resultCount;

    /// <summary>
    /// Liste der Bezirke (von API geladen)
    /// </summary>
    [ObservableProperty]
    private List<BezirkModel> _bezirke = [];

    public HomeFilterBarViewModel(IFilterStateService filterStateService, ILocationService locationService)
    {
        _filterStateService = filterStateService;
        _locationService = locationService;

        // Subscribe to filter state changes
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        // Subscribe to SellerType changes
        SubscribeToSellerTypeChanges();

        // Initialize from current state
        SyncFromFilterState();

        // Load locations from API
        _ = LoadLocationsAsync();
    }

    private async Task LoadLocationsAsync()
    {
        var locations = await _locationService.GetLocationsAsync();

        // Alle Bezirke mit Gemeinden aus allen Bundeslaendern extrahieren
        Bezirke = locations
            .SelectMany(bl => bl.Bezirke)
            .Select(b => new BezirkModel(
                b.Id,
                b.Name,
                b.Gemeinden.Select(g => new GemeindeModel(g.Id, g.Name, g.PostalCode)).ToList()
            ))
            .ToList();
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
            SelectedAgeFilter = state.SelectedAgeFilter;
            SelectedOrte = state.SelectedOrte.ToList();
            ResultCount = state.ResultCount;

            // SellerTypes synchronisieren
            UpdateSellerTypesFromState(state.IsPrivateSelected, state.IsBrokerSelected, state.IsPortalSelected);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void UpdateSellerTypesFromState(bool isPrivateSelected, bool isBrokerSelected, bool isPortalSelected)
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.IsSelected = sellerType.Type switch
            {
                SellerType.Private => isPrivateSelected,
                SellerType.Broker => isBrokerSelected,
                SellerType.Portal => isPortalSelected,
                _ => true
            };
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

    partial void OnSellerTypesChanged(List<SellerTypeModel> value)
    {
        // Bei SellerTypes-Aenderung auf PropertyChanged der einzelnen Items subscriben
        SubscribeToSellerTypeChanges();
    }

    private void SubscribeToSellerTypeChanges()
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.PropertyChanged += (s, e) =>
            {
                if (_isSyncing) return;
                if (e.PropertyName == nameof(SellerTypeModel.IsSelected))
                {
                    UpdateFilterState();
                }
            };
        }
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
        var (isPrivate, isBroker, isPortal) = GetSellerTypeSelection();

        _filterStateService.UpdateFilters(
            IsHausSelected,
            IsGrundstueckSelected,
            IsZwangsversteigerungSelected,
            SelectedAgeFilter,
            SelectedOrte,
            isPrivate,
            isBroker,
            isPortal);
    }

    private (bool IsPrivate, bool IsBroker, bool IsPortal) GetSellerTypeSelection()
    {
        return (
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Private)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Broker)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Portal)?.IsSelected ?? true
        );
    }
}
