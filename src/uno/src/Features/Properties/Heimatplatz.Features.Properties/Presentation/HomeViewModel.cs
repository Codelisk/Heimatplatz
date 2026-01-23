using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Navigation;

// ReSharper disable InconsistentNaming

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage
/// Implements INavigationAware for automatic lifecycle handling via BasePage
/// </summary>
public partial class HomeViewModel : ObservableObject, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly IMediator _mediator;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private ObservableCollection<PropertyListItemDto> _properties = new();

    [ObservableProperty]
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _isHausSelected;

    [ObservableProperty]
    private bool _isGrundstueckSelected;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected;

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
    /// Hierarchische Bezirk/Ort-Struktur für OÖ
    /// </summary>
    public List<BezirkModel> Bezirke { get; } =
    [
        new BezirkModel("Linz-Land", "Traun", "Leonding", "Ansfelden", "Pasching", "Hörsching"),
        new BezirkModel("Linz-Stadt", "Linz", "Urfahr"),
        new BezirkModel("Wels-Land", "Wels", "Marchtrenk", "Gunskirchen"),
        new BezirkModel("Steyr-Land", "Steyr", "Sierning", "Garsten"),
    ];

    public Visibility IsEmpty => Properties.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public HomeViewModel(
        IAuthService authService,
        INavigator navigator,
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        IMediator mediator)
    {
        _authService = authService;
        _navigator = navigator;
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _mediator = mediator;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        // Subscribe to filter state changes from HeaderFilterBar
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        UpdateAuthState();
        LoadTestData();

        // Load preferences if already authenticated
        if (_authService.IsAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
        }
    }

    #region INavigationAware Implementation

    /// <summary>
    /// Called by BasePage when navigated to (via INavigationAware)
    /// </summary>
    public async void OnNavigatedTo(object? parameter)
    {
        await SetupPageHeaderAsync();
    }

    /// <summary>
    /// Sets up page header via Mediator event and navigates to HeaderCenter Region
    /// </summary>
    private async Task SetupPageHeaderAsync()
    {
        // HomePage zeigt "HEIMATPLATZ" als Titel (null = Fallback)
        await _mediator.Publish(new PageHeaderChangedEvent(null));

        // Navigate to HomeFilterBar in the HeaderMain region
        // Use absolute path from Root since HeaderMain is in a different region hierarchy
        // Path: Root -> Main -> HeaderMain
        try
        {
            System.Diagnostics.Debug.WriteLine("[HomeViewModel] Navigating to HeaderMain with absolute path...");
            var response = await _navigator.NavigateRouteAsync(
                this,
                route: "HeaderMain",
                qualifier: Qualifiers.Root + "/Main");
            System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Navigation result: Success={response?.Success}, Route={response?.Route}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Navigation to HeaderMain failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Called by BasePage when navigated from (via INavigationAware)
    /// </summary>
    public void OnNavigatedFrom()
    {
        // HeaderMain will be cleared/replaced when next page navigates there
        // or stays with current content if next page doesn't navigate to HeaderMain
    }

    #endregion

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        UpdateAuthState();

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
            IsAllSelected = state.IsAllSelected;
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
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
            IsAllSelected = preferences.IsAllSelected;
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
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

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        if (value)
        {
            IsHausSelected = false;
            IsGrundstueckSelected = false;
            IsZwangsversteigerungSelected = false;
            ApplyFilters();
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        if (value)
        {
            IsAllSelected = false;
        }
        else
        {
            // Wenn alle deselektiert sind, setze "Alle" zurück
            if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            {
                IsAllSelected = true;
            }
        }
        ApplyFilters();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        if (value)
        {
            IsAllSelected = false;
        }
        else
        {
            // Wenn alle deselektiert sind, setze "Alle" zurück
            if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            {
                IsAllSelected = true;
            }
        }
        ApplyFilters();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isApplyingPreferences || _isSyncingFromService) return;

        if (value)
        {
            IsAllSelected = false;
        }
        else
        {
            // Wenn alle deselektiert sind, setze "Alle" zurück
            if (!IsHausSelected && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
            {
                IsAllSelected = true;
            }
        }
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Update FilterStateService (if not syncing from it)
        if (!_isSyncingFromService && !_isApplyingPreferences)
        {
            _filterStateService.UpdateFilters(
                IsAllSelected,
                IsHausSelected,
                IsGrundstueckSelected,
                IsZwangsversteigerungSelected,
                SelectedAgeFilter,
                SelectedOrte);
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

        Properties.Clear();
        var filteredList = filtered.ToList();
        System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Result count: {filteredList.Count}");
        foreach (var property in filteredList)
        {
            Properties.Add(property);
        }

        // Update result count in FilterStateService
        _filterStateService.SetResultCount(Properties.Count);

        OnPropertyChanged(nameof(IsEmpty));
    }

    private List<PropertyListItemDto> _allProperties = new();

    private void LoadTestData()
    {
        var now = DateTime.Now;

        _allProperties = new List<PropertyListItemDto>
        {
            // Created today
            new(Guid.NewGuid(), "Einfamilienhaus in Linz-Urfahr", "Hauptstrasse 15", "Linz", 349000, 145, 520, 5, PropertyType.House, SellerType.Makler, "Mustermann Immobilien",
                ["https://picsum.photos/seed/haus1a/800/600", "https://picsum.photos/seed/haus1b/800/600", "https://picsum.photos/seed/haus1c/800/600"],
                now.AddHours(-2), InquiryType.ContactData),

            // Created yesterday
            new(Guid.NewGuid(), "Modernes Reihenhaus in Wels", "Ringstrasse 42", "Wels", 289000, 120, 180, 4, PropertyType.House, SellerType.Privat, "Familie Huber",
                ["https://picsum.photos/seed/haus2a/800/600", "https://picsum.photos/seed/haus2b/800/600"],
                now.AddHours(-20), InquiryType.ContactData),

            // Created 5 days ago
            new(Guid.NewGuid(), "Familienhaus in Steyr", "Bahnhofstrasse 67", "Steyr", 315000, 135, 450, 5, PropertyType.House, SellerType.Makler, "Immobilien Steyr",
                ["https://picsum.photos/seed/haus3a/800/600", "https://picsum.photos/seed/haus3b/800/600", "https://picsum.photos/seed/haus3c/800/600", "https://picsum.photos/seed/haus3d/800/600"],
                now.AddDays(-5), InquiryType.ContactData),

            // Created 2 weeks ago
            new(Guid.NewGuid(), "Baugrundstück in Wels", "Neubaugebiet Sued", "Wels", 189000, null, 850, null, PropertyType.Land, SellerType.Privat, "Familie Mueller",
                ["https://picsum.photos/seed/grund1a/800/600", "https://picsum.photos/seed/grund1b/800/600"],
                now.AddDays(-14), InquiryType.ContactData),

            // Created 2 months ago
            new(Guid.NewGuid(), "Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000, null, 720, null, PropertyType.Land, SellerType.Makler, "Grund & Boden OOe",
                ["https://picsum.photos/seed/grund2a/800/600", "https://picsum.photos/seed/grund2b/800/600", "https://picsum.photos/seed/grund2c/800/600"],
                now.AddMonths(-2), InquiryType.ContactData),

            // Created 6 months ago
            new(Guid.NewGuid(), "Zwangsversteigerung: Haus in Traun", "Industriestrasse 45", "Traun", 185000, 110, 380, 4, PropertyType.Foreclosure, SellerType.Makler, "Bezirksgericht Linz",
                ["https://picsum.photos/seed/zwang1a/800/600", "https://picsum.photos/seed/zwang1b/800/600"],
                now.AddMonths(-6), InquiryType.ContactData),
        };

        ApplyFilters();
    }
}
