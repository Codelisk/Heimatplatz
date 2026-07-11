using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Notifications.Contracts.Models;
using Heimatplatz.Maui.Features.Notifications.Services;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Notifications.Presentation;

/// <summary>
/// ViewModel fuer die NotificationSettings-Seite.
/// Unterstuetzt 3 Filtermodi: All, SameAsSearch, Custom.
/// Einstellungen werden bei jeder Aenderung automatisch gespeichert.
/// </summary>
[ShellMap<NotificationSettingsPage>("NotificationSettings")]
public partial class NotificationSettingsViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly INotificationService _notificationService;
    private readonly ILocationService _locationService;
    private readonly ILogger<NotificationSettingsViewModel> _logger;

    // true bis zum ersten Laden, damit die Initialwerte aus dem Konstruktor
    // und dem Load keine Auto-Save-Aufrufe ausloesen
    private bool _isLoading = true;

    // Gemeinden fuer die Ort-Suche (cached vom LocationService)
    private List<LocationGemeindeDto> _municipalities = [];
    private bool _suppressSearch;

    public NotificationSettingsViewModel(
        INotificationService notificationService,
        ILocationService locationService,
        ILogger<NotificationSettingsViewModel> logger)
    {
        _notificationService = notificationService;
        _locationService = locationService;
        _logger = logger;

        SelectedOrte = [];
        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        IsFilterModeAll = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = true;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial NotificationFilterMode FilterMode { get; set; }

    // RadioButton bindings: Each is true when the corresponding mode is active
    [ObservableProperty]
    public partial bool IsFilterModeAll { get; set; }

    [ObservableProperty]
    public partial bool IsFilterModeSameAsSearch { get; set; }

    [ObservableProperty]
    public partial bool IsFilterModeCustom { get; set; }

    // Custom filter visibility
    public bool IsCustomFilterVisible => FilterMode == NotificationFilterMode.Custom;

    // Custom filter: Orte (Suche wie im Home-Filter, Auswahl als Chips)
    [ObservableProperty]
    public partial List<string> SelectedOrte { get; set; }

    public bool HasSelectedOrte => SelectedOrte.Count > 0;

    [ObservableProperty]
    public partial string OrtSearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrtSuggestions))]
    public partial List<LocationGemeindeDto> OrtSuggestions { get; set; }

    public bool HasOrtSuggestions => OrtSuggestions.Count > 0;

    // Custom filter: PropertyType
    [ObservableProperty]
    public partial bool IsHausSelected { get; set; }

    [ObservableProperty]
    public partial bool IsGrundstueckSelected { get; set; }

    [ObservableProperty]
    public partial bool IsZwangsversteigerungSelected { get; set; }

    // Custom filter: SellerType
    [ObservableProperty]
    public partial bool IsPrivateSelected { get; set; }

    [ObservableProperty]
    public partial bool IsBrokerSelected { get; set; }

    public void OnAppearing()
    {
        _ = LoadPreferencesAsync();
        _ = LoadMunicipalitiesAsync();
    }

    private async Task LoadMunicipalitiesAsync()
    {
        try
        {
            _municipalities = await _locationService.GetAllMunicipalitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemeinden fuer die Ort-Suche konnten nicht geladen werden");
        }
    }

    public void OnDisappearing()
    {
    }

    partial void OnFilterModeChanged(NotificationFilterMode value)
    {
        OnPropertyChanged(nameof(IsCustomFilterVisible));
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnIsFilterModeAllChanged(bool value)
    {
        if (!value) return;

        // Mutual exclusion (RadioButton-Semantik) im ViewModel absichern
        IsFilterModeSameAsSearch = false;
        IsFilterModeCustom = false;

        if (_isLoading) return;
        FilterMode = NotificationFilterMode.All;
        _ = SavePreferencesAsync();
    }

    partial void OnIsFilterModeSameAsSearchChanged(bool value)
    {
        if (!value) return;

        IsFilterModeAll = false;
        IsFilterModeCustom = false;

        if (_isLoading) return;
        FilterMode = NotificationFilterMode.SameAsSearch;
        _ = SavePreferencesAsync();
    }

    partial void OnIsFilterModeCustomChanged(bool value)
    {
        if (!value) return;

        IsFilterModeAll = false;
        IsFilterModeSameAsSearch = false;

        if (_isLoading) return;
        FilterMode = NotificationFilterMode.Custom;
        _ = SavePreferencesAsync();
    }

    partial void OnSelectedOrteChanged(List<string> value)
    {
        OnPropertyChanged(nameof(HasSelectedOrte));
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnOrtSearchTextChanged(string value)
    {
        if (_suppressSearch) return;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            OrtSuggestions = [];
            return;
        }

        var search = value.Trim();
        OrtSuggestions = _municipalities
            .Where(m => (m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                      || m.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                     && !SelectedOrte.Contains(m.Name))
            .Take(15)
            .ToList();
    }

    /// <summary>
    /// Fuegt einen Ort zur Auswahl hinzu (neue Liste, damit Auto-Save ausgeloest wird)
    /// </summary>
    [RelayCommand]
    private void AddOrt(LocationGemeindeDto gemeinde)
    {
        if (!SelectedOrte.Contains(gemeinde.Name))
        {
            SelectedOrte = [.. SelectedOrte, gemeinde.Name];
        }

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];
    }

    /// <summary>
    /// Entfernt einen Ort aus der Auswahl (neue Liste, damit Auto-Save ausgeloest wird)
    /// </summary>
    [RelayCommand]
    private void RemoveOrt(string ort)
    {
        if (SelectedOrte.Contains(ort))
        {
            SelectedOrte = SelectedOrte.Where(o => o != ort).ToList();
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isLoading) return;
        _ = SavePreferencesAsync();
    }

    /// <summary>
    /// Loads notification preferences from the API
    /// </summary>
    public async Task LoadPreferencesAsync()
    {
        try
        {
            _isLoading = true;
            IsBusy = true;

            var preferences = await _notificationService.GetPreferencesAsync(CancellationToken.None);

            IsEnabled = preferences.IsEnabled;
            FilterMode = preferences.FilterMode;

            // Set RadioButton states without triggering save
            IsFilterModeAll = preferences.FilterMode == NotificationFilterMode.All;
            IsFilterModeSameAsSearch = preferences.FilterMode == NotificationFilterMode.SameAsSearch;
            IsFilterModeCustom = preferences.FilterMode == NotificationFilterMode.Custom;

            SelectedOrte = preferences.Locations.ToList();
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
            IsPrivateSelected = preferences.IsPrivateSelected;
            IsBrokerSelected = preferences.IsBrokerSelected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notification preferences");
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves preferences to the API
    /// </summary>
    [RelayCommand]
    private async Task SavePreferencesAsync()
    {
        if (_isLoading) return;

        try
        {
            IsBusy = true;
            var success = await _notificationService.UpdatePreferencesAsync(
                IsEnabled,
                FilterMode,
                SelectedOrte,
                IsHausSelected,
                IsGrundstueckSelected,
                IsZwangsversteigerungSelected,
                IsPrivateSelected,
                IsBrokerSelected);
            if (success)
            {
                _logger.LogInformation("Notification preferences saved successfully");
            }
            else
            {
                _logger.LogWarning("Failed to save notification preferences");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving notification preferences");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
