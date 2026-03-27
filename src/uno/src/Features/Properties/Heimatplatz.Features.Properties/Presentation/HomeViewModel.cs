using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// PaginatedObservableCollection removed — using classic page navigation instead
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Heimatplatz.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
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

    private DispatcherQueue? _dispatcher;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    private ObservableCollection<PropertyListItemDto>? _properties;
    public ObservableCollection<PropertyListItemDto> Properties =>
        _properties ??= new();

    private const int PageSize = 20;
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageInfoText))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    private int _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageInfoText))]
    [NotifyPropertyChangedFor(nameof(PaginationVisibility))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    private int _totalPages;

    public bool HasPreviousPage => CurrentPage > 0;
    public bool HasNextPage => CurrentPage < TotalPages - 1;
    public string PageInfoText => TotalPages > 0 ? $"Seite {CurrentPage + 1} von {TotalPages}" : "";
    public Visibility PaginationVisibility => TotalPages > 1 ? Visibility.Visible : Visibility.Collapsed;

    public event EventHandler? PageChanged;

    private SortOption _selectedSort = SortOption.Neueste;
    public SortOption SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (SetProperty(ref _selectedSort, value))
            {
                OnPropertyChanged(nameof(SortLabel));
                _ = ReloadPropertiesAsync();
            }
        }
    }

    public string SortLabel => SelectedSort switch
    {
        SortOption.Aelteste => "Älteste",
        SortOption.PreisAuf => "Preis ↑",
        SortOption.PreisAb => "Preis ↓",
        SortOption.FlaecheAb => "Fläche ↓",
        SortOption.FlaecheAuf => "Fläche ↑",
        SortOption.PlzAuf => "PLZ",
        _ => "Neueste"
    };

    [RelayCommand]
    private void SetSort(string sortName)
    {
        SelectedSort = Enum.TryParse<SortOption>(sortName, out var option) ? option : SortOption.Neueste;
    }

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

    /// <summary>
    /// Liste der Anbietertypen fuer den SellerTypePicker (Mobile)
    /// </summary>
    [ObservableProperty]
    private List<SellerTypeModel> _sellerTypes = SellerTypeModel.CreateDefaultList();

    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    public AgeFilter SelectedAgeFilter
    {
        get => _selectedAgeFilter;
        set
        {
            if (SetProperty(ref _selectedAgeFilter, value))
            {
                _ = ReloadPropertiesAsync();
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

    private List<Guid> _selectedMunicipalityIds = new();
    /// <summary>
    /// Selected municipality IDs for filtering (replaces SelectedOrte strings)
    /// </summary>
    public List<Guid> SelectedMunicipalityIds
    {
        get => _selectedMunicipalityIds;
        set
        {
            if (SetProperty(ref _selectedMunicipalityIds, value))
            {
                _ = ReloadPropertiesAsync();
            }
        }
    }

    // Keep SelectedOrte for backward compatibility with UI that uses string names
    // This property is synced from SelectedMunicipalityIds
    private List<string> _selectedOrte = new();
    public List<string> SelectedOrte
    {
        get => _selectedOrte;
        set
        {
            if (SetProperty(ref _selectedOrte, value))
            {
                // Convert names to IDs using Bezirke lookup
                var ids = Bezirke
                    .SelectMany(b => b.Gemeinden)
                    .Where(g => value.Contains(g.Name))
                    .Select(g => g.Id)
                    .ToList();
                SelectedMunicipalityIds = ids;
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
    private Visibility _emptyVisibility = Visibility.Collapsed;

    partial void OnIsEmptyChanged(bool value)
    {
        EmptyVisibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

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

        // Subscribe to SellerType changes from SellerTypePicker
        SubscribeToSellerTypeChanges();

        UpdateAuthState();

        // Load locations from API
        _ = LoadLocationsAsync();

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
        // Capture UI dispatcher (OnNavigatedTo always runs on the UI thread)
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();

        // Always sync filter values from FilterStateService first.
        // This preserves session filter state when ViewModel is recreated on navigation.
        SyncFiltersFromService();

        if (Properties.Count == 0 && !IsBusy)
        {
            _ = LoadCurrentPageAsync();
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
        _ = ReloadPropertiesAsync();

        if (isAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
        }
    }

    /// <summary>
    /// Syncs filter values from FilterStateService without triggering a reload.
    /// Called at the start of OnNavigatedTo to restore session state.
    /// Only syncs if session state has been set (filters were changed at least once).
    /// </summary>
    private void SyncFiltersFromService()
    {
        if (!_filterStateService.HasSessionState) return;

        var state = _filterStateService.CurrentState;

        _isSyncingFromService = true;
        try
        {
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
            IsPrivateSelected = state.IsPrivateSelected;
            IsBrokerSelected = state.IsBrokerSelected;
            IsPortalSelected = state.IsPortalSelected;
            SetProperty(ref _selectedAgeFilter, state.SelectedAgeFilter, nameof(SelectedAgeFilter));
            SetProperty(ref _selectedOrte, state.SelectedOrte.ToList(), nameof(SelectedOrte));
            SetProperty(ref _selectedSort, state.SelectedSort, nameof(SelectedSort));
            OnPropertyChanged(nameof(SortLabel));

            var ids = Bezirke
                .SelectMany(b => b.Gemeinden)
                .Where(g => state.SelectedOrte.Contains(g.Name))
                .Select(g => g.Id)
                .ToList();
            SetProperty(ref _selectedMunicipalityIds, ids, nameof(SelectedMunicipalityIds));

            UpdateSellerTypesFromBools();
        }
        finally
        {
            _isSyncingFromService = false;
        }
    }

    /// <summary>
    /// Restores filter UI from in-memory FilterStateService.
    /// Only reloads properties if state actually changed (e.g. from FilterPreferencesPage).
    /// </summary>
    private void RestoreFromFilterState()
    {
        var state = _filterStateService.CurrentState;

        // Check if any filter actually differs from current ViewModel state
        var changed = state.IsHausSelected != IsHausSelected
            || state.IsGrundstueckSelected != IsGrundstueckSelected
            || state.IsZwangsversteigerungSelected != IsZwangsversteigerungSelected
            || state.IsPrivateSelected != IsPrivateSelected
            || state.IsBrokerSelected != IsBrokerSelected
            || state.IsPortalSelected != IsPortalSelected
            || state.SelectedAgeFilter != SelectedAgeFilter
            || !state.SelectedOrte.SequenceEqual(SelectedOrte);

        if (!changed) return;

        // State differs (e.g. FilterPreferencesPage updated it) — apply and reload
        _isSyncingFromService = true;
        try
        {
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
            IsPrivateSelected = state.IsPrivateSelected;
            IsBrokerSelected = state.IsBrokerSelected;
            IsPortalSelected = state.IsPortalSelected;
            SetProperty(ref _selectedAgeFilter, state.SelectedAgeFilter, nameof(SelectedAgeFilter));
            SetProperty(ref _selectedOrte, state.SelectedOrte.ToList(), nameof(SelectedOrte));

            var ids = Bezirke
                .SelectMany(b => b.Gemeinden)
                .Where(g => state.SelectedOrte.Contains(g.Name))
                .Select(g => g.Id)
                .ToList();
            SetProperty(ref _selectedMunicipalityIds, ids, nameof(SelectedMunicipalityIds));

            UpdateSellerTypesFromBools();
        }
        finally
        {
            _isSyncingFromService = false;
        }

        _ = ReloadPropertiesAsync();
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
            SetProperty(ref _selectedAgeFilter, state.SelectedAgeFilter, nameof(SelectedAgeFilter));
            SetProperty(ref _selectedOrte, state.SelectedOrte.ToList(), nameof(SelectedOrte));
            SetProperty(ref _selectedSort, state.SelectedSort, nameof(SelectedSort));
            OnPropertyChanged(nameof(SortLabel));

            // Convert names to IDs for server-side filtering
            var ids = Bezirke
                .SelectMany(b => b.Gemeinden)
                .Where(g => state.SelectedOrte.Contains(g.Name))
                .Select(g => g.Id)
                .ToList();
            SetProperty(ref _selectedMunicipalityIds, ids, nameof(SelectedMunicipalityIds));

            // Sync SellerTypes list from bools (while _isSyncingFromService is true,
            // so the SellerType PropertyChanged subscription won't re-trigger)
            UpdateSellerTypesFromBools();

            // Reload with new filters (server-side)
            _ = ReloadPropertiesAsync();
        }
        finally
        {
            _isSyncingFromService = false;
        }
    }

    private bool _isSyncingFromService;

    private void SubscribeToSellerTypeChanges()
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.PropertyChanged += (s, e) =>
            {
                if (_isSyncingFromService || _isApplyingPreferences) return;
                if (e.PropertyName == nameof(SellerTypeModel.IsSelected))
                {
                    SyncBoolsFromSellerTypes();
                }
            };
        }
    }

    private void SyncBoolsFromSellerTypes()
    {
        _isSyncingFromService = true;
        try
        {
            IsPrivateSelected = SellerTypes.FirstOrDefault(st => st.Type == SellerType.Private)?.IsSelected ?? true;
            IsBrokerSelected = SellerTypes.FirstOrDefault(st => st.Type == SellerType.Broker)?.IsSelected ?? true;
            IsPortalSelected = SellerTypes.FirstOrDefault(st => st.Type == SellerType.Portal)?.IsSelected ?? true;
        }
        finally
        {
            _isSyncingFromService = false;
        }

        OnFiltersChanged();
    }

    private void UpdateSellerTypesFromBools()
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.IsSelected = sellerType.Type switch
            {
                SellerType.Private => IsPrivateSelected,
                SellerType.Broker => IsBrokerSelected,
                SellerType.Portal => IsPortalSelected,
                _ => true
            };
        }
    }

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
        SetProperty(ref _selectedSort, preferences.SelectedSort, nameof(SelectedSort));
        OnPropertyChanged(nameof(SortLabel));

        // Convert names to IDs for server-side filtering
        var ids = Bezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => preferences.SelectedOrte.Contains(g.Name))
            .Select(g => g.Id)
            .ToList();
        SetProperty(ref _selectedMunicipalityIds, ids, nameof(SelectedMunicipalityIds));

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

            // Sync SellerTypes list from preference bools
            UpdateSellerTypesFromBools();
        }
        finally
        {
            _isApplyingPreferences = false;
        }

        // Reload with new filters (server-side)
        _ = ReloadPropertiesAsync();
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

        OnFiltersChanged();
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

        OnFiltersChanged();
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

        OnFiltersChanged();
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

        OnFiltersChanged();
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

        OnFiltersChanged();
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

        OnFiltersChanged();
    }

    /// <summary>
    /// Called when any filter changes - updates FilterStateService and triggers server reload
    /// </summary>
    private void OnFiltersChanged()
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
                IsPortalSelected,
                selectedSort: SelectedSort);
        }

        // Trigger server-side reload with new filters
        _ = ReloadPropertiesAsync();
    }

    /// <summary>
    /// Loads the current page from the API and replaces collection contents
    /// </summary>
    private async Task LoadCurrentPageAsync()
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilien...";
        try
        {
            var (items, _, totalCount) = await LoadPageAsync(CurrentPage, PageSize, CancellationToken.None);
            var itemList = items.ToList();

            Properties.Clear();
            foreach (var item in itemList)
                Properties.Add(item);

            _totalCount = totalCount;
            TotalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / PageSize) : 0;
            DispatchToUI(UpdateResultCount);
            PageChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private async Task GoToNextPageAsync()
    {
        CurrentPage++;
        await LoadCurrentPageAsync();
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        CurrentPage--;
        await LoadCurrentPageAsync();
    }

    /// <summary>
    /// Loads a page of properties from the API
    /// </summary>
    private async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> LoadPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        _logger.LogInformation("[HomePage] Loading page {Page} with pageSize {PageSize}", page, pageSize);

        try
        {
            // Build API request with all server-side filters
            // Map SortOption to API parameters
            var (sortBy, sortDesc) = SelectedSort switch
            {
                SortOption.Aelteste => ("CreatedAt", false),
                SortOption.PreisAuf => ("Price", false),
                SortOption.PreisAb => ("Price", true),
                SortOption.FlaecheAb => ("PlotArea", true),
                SortOption.FlaecheAuf => ("PlotArea", false),
                SortOption.PlzAuf => ("PostalCode", false),
                _ => ((string?)null, true)
            };

            var request = new Heimatplatz.Core.ApiClient.Generated.GetPropertiesHttpRequest
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDesc
            };

            // PropertyTypes filter (multi-select as JSON array)
            var selectedPropertyTypes = new List<string>();
            if (IsHausSelected) selectedPropertyTypes.Add("House");
            if (IsGrundstueckSelected) selectedPropertyTypes.Add("Land");
            if (IsZwangsversteigerungSelected) selectedPropertyTypes.Add("Foreclosure");
            if (selectedPropertyTypes.Count > 0 && selectedPropertyTypes.Count < 3)
            {
                request.PropertyTypesJson = JsonSerializer.Serialize(selectedPropertyTypes);
            }

            // SellerTypes filter (multi-select as JSON array)
            var selectedSellerTypes = new List<string>();
            if (IsPrivateSelected) selectedSellerTypes.Add("Private");
            if (IsBrokerSelected) selectedSellerTypes.Add("Broker");
            if (IsPortalSelected) selectedSellerTypes.Add("Portal");
            if (selectedSellerTypes.Count > 0 && selectedSellerTypes.Count < 3)
            {
                request.SellerTypesJson = JsonSerializer.Serialize(selectedSellerTypes);
            }

            // MunicipalityIds filter (multi-select as JSON array)
            if (SelectedMunicipalityIds.Count > 0)
            {
                request.MunicipalityIdsJson = JsonSerializer.Serialize(SelectedMunicipalityIds);
            }

            // CreatedAfter filter (age filter)
            if (SelectedAgeFilter != AgeFilter.Alle)
            {
                var cutoffDate = SelectedAgeFilter switch
                {
                    AgeFilter.EinTag => DateTimeOffset.UtcNow.AddDays(-1),
                    AgeFilter.EineWoche => DateTimeOffset.UtcNow.AddDays(-7),
                    AgeFilter.EinMonat => DateTimeOffset.UtcNow.AddMonths(-1),
                    AgeFilter.EinJahr => DateTimeOffset.UtcNow.AddYears(-1),
                    _ => DateTimeOffset.MinValue
                };
                request.CreatedAfter = cutoffDate;
                _logger.LogWarning("[HomePage] AgeFilter={Filter}, CreatedAfter={CreatedAfter:O}", SelectedAgeFilter, cutoffDate);
            }

            var (_, response) = await _mediator.Request(request, ct);

            _logger.LogInformation("[HomePage] Response received. Properties count: {Count}, HasMore: {HasMore}",
                response?.Properties?.Count ?? 0, response?.HasMore ?? false);

            // Map API response to DTOs
            var items = response?.Properties?.Select(prop => new PropertyListItemDto(
                Id: prop.Id,
                Title: prop.Title,
                Address: prop.Address,
                MunicipalityId: prop.MunicipalityId,
                City: prop.City,
                PostalCode: prop.PostalCode,
                Price: (decimal)prop.Price,
                LivingAreaM2: prop.LivingAreaM2,
                PlotAreaM2: prop.PlotAreaM2,
                Rooms: prop.Rooms,
                Type: Enum.Parse<PropertyType>(prop.Type.ToString()),
                SellerType: Enum.Parse<SellerType>(prop.SellerType.ToString()),
                SellerName: prop.SellerName,
                ImageUrls: prop.ImageUrls,
                CreatedAt: prop.CreatedAt.DateTime,
                InquiryType: Enum.Parse<InquiryType>(prop.InquiryType.ToString()),
                SourceName: prop.SourceName
            )) ?? Enumerable.Empty<PropertyListItemDto>();

            var hasMore = response?.HasMore ?? false;
            var totalCount = response?.Total ?? 0;

            return (items, hasMore, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] Error loading page {Page}", page);
            return (Enumerable.Empty<PropertyListItemDto>(), false, 0);
        }
    }

    /// <summary>
    /// Reloads properties from API (resets collection and loads first page with current filters)
    /// </summary>
    private async Task ReloadPropertiesAsync()
    {
        if (_properties == null) return;

        _logger.LogInformation("[HomePage] Reloading properties with current filters");
        CurrentPage = 0;
        await LoadCurrentPageAsync();
    }

    /// <summary>
    /// Updates the result count text and empty state
    /// </summary>
    private void UpdateResultCount()
    {
        IsEmpty = _totalCount == 0;
        ResultCountText = $"{_totalCount} Objekte";
        _filterStateService.SetResultCount(_totalCount);
    }

    /// <summary>
    /// Reloads properties from API (command for UI binding)
    /// </summary>
    [RelayCommand]
    private async Task RefreshPropertiesAsync()
    {
        await ReloadPropertiesAsync();
    }

    /// <summary>
    /// Dispatches an action to the UI thread. Falls back to direct invocation
    /// if dispatcher is not yet available.
    /// </summary>
    private void DispatchToUI(Action action)
    {
        var dq = _dispatcher;
        if (dq is not null)
            dq.TryEnqueue(() => action());
        else
            action();
    }
}
