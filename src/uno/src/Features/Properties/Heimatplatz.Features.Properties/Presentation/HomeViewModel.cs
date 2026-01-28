using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Collections;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Heimatplatz.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Navigation;

// ReSharper disable InconsistentNaming

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage
/// Implements INavigationAware for automatic lifecycle handling via BasePage
/// Implements IPageInfo for header integration
/// </summary>
public partial class HomeViewModel : ObservableObject, INavigationAware, IPageInfo
{
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly IMediator _mediator;
    private readonly ILocationService _locationService;
    private readonly ILogger<HomeViewModel> _logger;

    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    // Use a stable collection reference to avoid StackOverflow from binding system
    // when both old (being torn down) and new page instances react to PropertyChanged.
    // Singleton ViewModel + NavigationCacheMode="Disabled" means the old page's bindings
    // may still be alive when Properties is reassigned, causing recursive cascades.
    private readonly BatchObservableCollection<PropertyListItemDto> _properties = new();
    public BatchObservableCollection<PropertyListItemDto> Properties => _properties;

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

    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    public AgeFilter SelectedAgeFilter
    {
        get => _selectedAgeFilter;
        set
        {
            if (SetProperty(ref _selectedAgeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    private bool _isAuthenticated;

    /// <summary>
    /// Inverse von IsAuthenticated fuer XAML-Binding
    /// </summary>
    public bool IsNotAuthenticated => !IsAuthenticated;

    [ObservableProperty]
    private string? _userFullName;

    [ObservableProperty]
    private string? _userInitials;

    [ObservableProperty]
    private string? _userFirstInitial;

    [ObservableProperty]
    private string? _userDisplayName;

    private List<string> _selectedOrte = new();
    public List<string> SelectedOrte
    {
        get => _selectedOrte;
        set
        {
            if (SetProperty(ref _selectedOrte, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Liste der Bezirke (von API geladen)
    /// </summary>
    [ObservableProperty]
    private List<BezirkModel> _bezirke = [];

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string _resultCountText = "0 Objekte";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterToggleGlyph))]
    private bool _isFilterExpanded;

    /// <summary>
    /// Chevron-Glyph fuer den Filter-Toggle-Button
    /// </summary>
    public string FilterToggleGlyph => IsFilterExpanded ? "\uE70E" : "\uE70D";

    #region IPageInfo Implementation

    public PageType PageType => PageType.Home;
    public string PageTitle => "HEIMATPLATZ";
    public Type? MainHeaderViewModel => typeof(HomeFilterBarViewModel);

    #endregion

    public HomeViewModel(
        IAuthService authService,
        INavigator navigator,
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        IMediator mediator,
        ILocationService locationService,
        ILogger<HomeViewModel> logger)
    {
        _authService = authService;
        _navigator = navigator;
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _mediator = mediator;
        _locationService = locationService;
        _logger = logger;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        // Subscribe to filter state changes from HeaderFilterBar
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        UpdateAuthState();

        // Load locations from API
        _ = LoadLocationsAsync();

        // Load properties from API
        _ = LoadPropertiesAsync();

        // Load preferences if already authenticated
        // PropertyStatusService is loaded lazily via EnsureLoadedAsync() in OnPropertyCardLoaded
        if (_authService.IsAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
        }
    }

    #region INavigationAware Implementation

    /// <summary>
    /// Called by BasePage when navigated to (via INavigationAware).
    /// Header updates are now automatic via PageNavigatedEvent from BasePage.
    /// </summary>
    public void OnNavigatedTo(object? parameter)
    {
        // Only reload if we don't have data yet.
        // Re-navigating back to an already-loaded Home page should NOT trigger
        // _properties.Reset() because the old page's ItemsRepeater is still alive
        // and bound to the same collection (singleton ViewModel), which would cause
        // both old and new pages to rebuild their PropertyCards simultaneously → StackOverflow.
        if (_allProperties.Count == 0)
        {
            _ = LoadPropertiesAsync();
        }
    }

    /// <summary>
    /// Called by BasePage when navigated from (via INavigationAware)
    /// </summary>
    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    #endregion

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        UpdateAuthState();

        // Reload properties when auth state changes (blocked properties filter depends on auth)
        _ = LoadPropertiesAsync();

        if (isAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
        }
    }

    private void OnFilterStateChanged(object? sender, EventArgs e)
    {
        // Sync local state from FilterStateService (when changed from HeaderFilterBar)
        _isSyncingFromService = true;
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

            // Re-apply filters with synced state
            ApplyFiltersInternal();
        }
        finally
        {
            _isSyncingFromService = false;
        }
    }

    private bool _isSyncingFromService;

    private async Task LoadLocationsAsync()
    {
        try
        {
            var locations = await _locationService.GetLocationsAsync();
            Bezirke = locations
                .SelectMany(bl => bl.Bezirke)
                .Select(b => new BezirkModel(
                    b.Id,
                    b.Name,
                    b.Gemeinden.Select(g => new GemeindeModel(g.Id, g.Name, g.PostalCode)).ToList()
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] Failed to load locations from API");
        }
    }

    private async Task LoadFilterPreferencesAsync()
    {
        try
        {
            var preferences = await _filterPreferencesService.GetPreferencesAsync();
            if (preferences != null)
            {
                ApplyFilterPreferences(preferences);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomePage] Failed to load filter preferences: {ex.Message}");
        }
    }

    private void ApplyFilterPreferences(FilterPreferencesDto preferences)
    {
        // Use SetProperty to update backing fields properly
        SetProperty(ref _selectedOrte, preferences.SelectedOrte.ToList(), nameof(SelectedOrte));
        SetProperty(ref _selectedAgeFilter, preferences.SelectedAgeFilter, nameof(SelectedAgeFilter));

        // For ObservableProperty fields, set through generated properties
        // but suppress filter application by using a flag
        _isApplyingPreferences = true;
        try
        {
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
            IsPrivateSelected = preferences.IsPrivateSelected;
            IsBrokerSelected = preferences.IsBrokerSelected;
            IsPortalSelected = preferences.IsPortalSelected;
        }
        finally
        {
            _isApplyingPreferences = false;
        }

        // Apply filters once
        ApplyFilters();
    }

    private bool _isApplyingPreferences;

    private void UpdateAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated;
        UserFullName = _authService.UserFullName;
        UserInitials = GetInitials(_authService.UserFullName);
        UserFirstInitial = GetFirstInitial(_authService.UserFullName);
        UserDisplayName = GetDisplayName(_authService.UserFullName);
    }

    private static string? GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    /// <summary>
    /// Erstes Initial fuer Monogramm-Anzeige (z.B. "M")
    /// </summary>
    private static string? GetFirstInitial(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        return fullName.Trim()[0].ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Abgekuerzter Anzeigename fuer elegante Darstellung (z.B. "M. Schmidt")
    /// </summary>
    private static string? GetDisplayName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (parts.Length == 1)
            return parts[0];

        // Format: "M. Nachname"
        return $"{parts[0][0]}. {parts[^1]}";
    }

    [RelayCommand]
    private void ToggleFilterExpanded()
    {
        IsFilterExpanded = !IsFilterExpanded;
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.ClearAuthentication();
    }

    [RelayCommand]
    private async Task NavigateToAddPropertyAsync()
    {
        await _navigator.NavigateViewModelAsync<AddPropertyViewModel>(this);
    }

    [RelayCommand]
    private async Task NavigateToFavoritesAsync()
    {
        await _navigator.NavigateViewModelAsync<FavoritesViewModel>(this);
    }

    [RelayCommand]
    private async Task NavigateToBlockedAsync()
    {
        await _navigator.NavigateViewModelAsync<BlockedViewModel>(this);
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncingFromService = true;
            IsHausSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncingFromService = true;
            IsGrundstueckSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsGrundstueckSelected)
        {
            _isSyncingFromService = true;
            IsZwangsversteigerungSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsBrokerSelected && !IsPortalSelected)
        {
            _isSyncingFromService = true;
            IsPrivateSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsPrivateSelected && !IsPortalSelected)
        {
            _isSyncingFromService = true;
            IsBrokerSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    partial void OnIsPortalSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        // Mindestens ein SellerType muss aktiv bleiben
        if (!value && !IsPrivateSelected && !IsBrokerSelected)
        {
            _isSyncingFromService = true;
            IsPortalSelected = true;
            _isSyncingFromService = false;
            return;
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Update FilterStateService (if not syncing from it)
        if (!_isSyncingFromService && !_isApplyingPreferences)
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

        ApplyFiltersInternal();
    }

    private void ApplyFiltersInternal()
    {
        System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Called. SelectedOrte.Count = {SelectedOrte.Count}");
        var filtered = _allProperties.AsEnumerable();

        // City filter (Multi-Select)
        if (SelectedOrte.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by cities: {string.Join(", ", SelectedOrte)}");
            filtered = filtered.Where(p => SelectedOrte.Contains(p.City));
        }

        // Age filter
        if (SelectedAgeFilter != AgeFilter.Alle)
        {
            var cutoffDate = SelectedAgeFilter switch
            {
                AgeFilter.EinTag => DateTime.Now.AddDays(-1),
                AgeFilter.EineWoche => DateTime.Now.AddDays(-7),
                AgeFilter.EinMonat => DateTime.Now.AddMonths(-1),
                AgeFilter.EinJahr => DateTime.Now.AddYears(-1),
                _ => DateTime.MinValue
            };
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by age: {SelectedAgeFilter}, cutoff: {cutoffDate}");
            filtered = filtered.Where(p => p.CreatedAt >= cutoffDate);
        }

        // Type filter (Multi-Select mit OR-Logik)
        var selectedTypes = new List<PropertyType>();
        if (IsHausSelected) selectedTypes.Add(PropertyType.House);
        if (IsGrundstueckSelected) selectedTypes.Add(PropertyType.Land);
        if (IsZwangsversteigerungSelected) selectedTypes.Add(PropertyType.Foreclosure);

        if (selectedTypes.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by types: {string.Join(", ", selectedTypes)}");
            filtered = filtered.Where(p => selectedTypes.Contains(p.Type));
        }

        // SellerType filter (Multi-Select mit OR-Logik)
        var selectedSellerTypes = new List<SellerType>();
        if (IsPrivateSelected) selectedSellerTypes.Add(SellerType.Private);
        if (IsBrokerSelected) selectedSellerTypes.Add(SellerType.Broker);
        if (IsPortalSelected) selectedSellerTypes.Add(SellerType.Portal);

        if (selectedSellerTypes.Count > 0 && selectedSellerTypes.Count < 3)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by seller types: {string.Join(", ", selectedSellerTypes)}");
            filtered = filtered.Where(p => selectedSellerTypes.Contains(p.SellerType));
        }

        var filteredList = filtered.ToList();
        System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Result count: {filteredList.Count}");

        // Batch-replace all items with a single Reset notification.
        // Individual Add() calls would fire N CollectionChanged events, each triggering
        // a full ItemsRepeater render cycle through Uno's DependencyProperty invalidation,
        // which cascades into a StackOverflow.
        _properties.Reset(filteredList);

        // Update IsEmpty explicitly — using a bool [ObservableProperty] instead of a computed
        // Visibility property avoids the binding system subscribing to CollectionChanged on
        // Properties.Count, which would create a self-referential loop.
        IsEmpty = filteredList.Count == 0;

        // Update the result count text for XAML binding.
        // IMPORTANT: Do NOT bind directly to Properties.Count in XAML — that subscribes the
        // binding system to CollectionChanged, creating a recursive cascade through
        // DependencyObjectStore → BindingExpression → OnValueChanged → StackOverflow.
        ResultCountText = $"{filteredList.Count} Objekte";

        // Update result count in FilterStateService
        _filterStateService.SetResultCount(filteredList.Count);
    }

    private List<PropertyListItemDto> _allProperties = new();

    /// <summary>
    /// Loads properties from the API
    /// </summary>
    private async Task LoadPropertiesAsync()
    {
        if (_isLoading)
        {
            _logger.LogInformation("[HomePage] LoadPropertiesAsync skipped - already loading");
            return;
        }

        _isLoading = true;
        IsBusy = true;
        BusyMessage = "Lade Immobilien...";

        try
        {
            _logger.LogInformation("[HomePage] Starting to load properties from API");

            // Build API request with server-side filters (PropertyType)
            var request = new Heimatplatz.Core.ApiClient.Generated.GetPropertiesHttpRequest
            {
                Take = 100 // Load more properties for local filtering
            };

            // Apply type filter if only one type is selected (server-side optimization)
            // API supports only one type at a time, so we'll filter locally for multi-select
            if (IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            {
                request.Type = Heimatplatz.Core.ApiClient.Generated.PropertyType.House;
            }
            else if (IsGrundstueckSelected && !IsHausSelected && !IsZwangsversteigerungSelected)
            {
                request.Type = Heimatplatz.Core.ApiClient.Generated.PropertyType.Land;
            }
            else if (IsZwangsversteigerungSelected && !IsHausSelected && !IsGrundstueckSelected)
            {
                request.Type = Heimatplatz.Core.ApiClient.Generated.PropertyType.Foreclosure;
            }

            // Apply city filter if only one city is selected
            if (SelectedOrte.Count == 1)
            {
                request.City = SelectedOrte[0];
            }

            var (context, response) = await _mediator.Request(request);

            _logger.LogInformation("[HomePage] Response received. Properties count: {Count}", response?.Properties?.Count ?? 0);

            // Clear and reload all properties
            _allProperties.Clear();

            if (response?.Properties != null)
            {
                foreach (var prop in response.Properties)
                {
                    _allProperties.Add(new PropertyListItemDto(
                        Id: prop.Id,
                        Title: prop.Title,
                        Address: prop.Address,
                        City: prop.City,
                        Price: (decimal)prop.Price,
                        LivingAreaM2: prop.LivingAreaM2,
                        PlotAreaM2: prop.PlotAreaM2,
                        Rooms: prop.Rooms,
                        Type: Enum.Parse<PropertyType>(prop.Type.ToString()),
                        SellerType: Enum.Parse<SellerType>(prop.SellerType.ToString()),
                        SellerName: prop.SellerName,
                        ImageUrls: prop.ImageUrls,
                        CreatedAt: prop.CreatedAt.DateTime,
                        InquiryType: Enum.Parse<InquiryType>(prop.InquiryType.ToString())
                    ));
                }
            }

            // Apply local filters (for multi-select types, age filter, multi-city)
            ApplyFiltersInternal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] Error loading properties from API");
            // Keep existing properties on error
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
            _isLoading = false;
        }
    }

    /// <summary>
    /// Reloads properties from API (called when filters change that require server-side filtering)
    /// </summary>
    [RelayCommand]
    private async Task RefreshPropertiesAsync()
    {
        await LoadPropertiesAsync();
    }
}
