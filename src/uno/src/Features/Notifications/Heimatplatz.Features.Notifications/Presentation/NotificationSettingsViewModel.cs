using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Notifications.Contracts.Models;
using Heimatplatz.Features.Notifications.Services;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Notifications.Presentation;

/// <summary>
/// ViewModel for NotificationSettings page using MVVM pattern
/// Supports 3 filter modes: All, SameAsSearch, Custom
/// </summary>
public partial class NotificationSettingsViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly INotificationService _notificationService;
    private readonly ILocationService _locationService;
    private readonly ILogger<NotificationSettingsViewModel> _logger;
    private bool _isLoading;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Settings;
    public string PageTitle => "Benachrichtigungen";
    public Type? MainHeaderViewModel => null;

    #endregion

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadPreferencesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    #endregion

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private NotificationFilterMode _filterMode = NotificationFilterMode.All;

    // RadioButton bindings: Each is true when the corresponding mode is active
    [ObservableProperty]
    private bool _isFilterModeAll = true;

    [ObservableProperty]
    private bool _isFilterModeSameAsSearch;

    [ObservableProperty]
    private bool _isFilterModeCustom;

    // Custom filter visibility
    public bool IsCustomFilterVisible => FilterMode == NotificationFilterMode.Custom;

    // Custom filter: Locations (OrtPicker)
    [ObservableProperty]
    private List<BezirkModel> _bezirke = [];

    [ObservableProperty]
    private List<string> _selectedOrte = [];

    // Custom filter: PropertyType
    [ObservableProperty]
    private bool _isHausSelected = true;

    [ObservableProperty]
    private bool _isGrundstueckSelected = true;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected = true;

    // Custom filter: SellerType
    [ObservableProperty]
    private bool _isPrivateSelected = true;

    [ObservableProperty]
    private bool _isBrokerSelected = true;

    [ObservableProperty]
    private bool _isPortalSelected = true;

    public NotificationSettingsViewModel(
        INotificationService notificationService,
        ILocationService locationService,
        ILogger<NotificationSettingsViewModel> logger)
    {
        _notificationService = notificationService;
        _locationService = locationService;
        _logger = logger;
    }

    partial void OnFilterModeChanged(NotificationFilterMode value)
    {
        OnPropertyChanged(nameof(IsCustomFilterVisible));
    }

    partial void OnIsFilterModeAllChanged(bool value)
    {
        if (_isLoading || !value) return;
        FilterMode = NotificationFilterMode.All;
        _ = SavePreferencesAsync();
    }

    partial void OnIsFilterModeSameAsSearchChanged(bool value)
    {
        if (_isLoading || !value) return;
        FilterMode = NotificationFilterMode.SameAsSearch;
        _ = SavePreferencesAsync();
    }

    partial void OnIsFilterModeCustomChanged(bool value)
    {
        if (_isLoading || !value) return;
        FilterMode = NotificationFilterMode.Custom;
        _ = SavePreferencesAsync();
    }

    partial void OnSelectedOrteChanged(List<string> value)
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

            // Load Bezirke for OrtPicker
            var locations = await _locationService.GetLocationsAsync();
            Bezirke = locations
                .SelectMany(bl => bl.Bezirke)
                .Select(b => new BezirkModel(
                    b.Id,
                    b.Name,
                    b.Gemeinden.Select(g => new GemeindeModel(g.Id, g.Name, g.PostalCode)).ToList()
                ))
                .ToList();

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
            IsPortalSelected = preferences.IsPortalSelected;
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
    /// Toggles notifications enabled/disabled
    /// </summary>
    [RelayCommand]
    private async Task ToggleEnabledAsync()
    {
        await SavePreferencesAsync();
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
                IsBrokerSelected,
                IsPortalSelected);
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
