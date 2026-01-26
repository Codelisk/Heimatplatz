using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Notifications.Contracts.Models;
using Heimatplatz.Features.Notifications.Services;
using Microsoft.Extensions.Logging;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Notifications.Presentation;

/// <summary>
/// ViewModel for NotificationSettings page using MVVM pattern
/// Implements INavigationAware to trigger PageNavigatedEvent for header updates
/// </summary>
public partial class NotificationSettingsViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationSettingsViewModel> _logger;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Settings;
    public string PageTitle => "Benachrichtigungen";
    public Type? MainHeaderViewModel => null;

    #endregion

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // Load preferences when navigated to
        _ = LoadPreferencesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    #endregion

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private List<string> _locations = new();

    [ObservableProperty]
    private string _newLocation = string.Empty;

    [ObservableProperty]
    private bool _isPrivateSelected = true;

    [ObservableProperty]
    private bool _isBrokerSelected = true;

    [ObservableProperty]
    private bool _isPortalSelected = true;

    public NotificationSettingsViewModel(
        INotificationService notificationService,
        ILogger<NotificationSettingsViewModel> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Loads notification preferences from the API
    /// </summary>
    public async Task LoadPreferencesAsync()
    {
        try
        {
            IsBusy = true;
            var preferences = await _notificationService.GetPreferencesAsync(CancellationToken.None);
            IsEnabled = preferences.IsEnabled;
            Locations = new List<string>(preferences.Locations);
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
            IsBusy = false;
        }
    }

    /// <summary>
    /// Adds a new location to the preferences
    /// </summary>
    [RelayCommand]
    private async Task AddLocationAsync()
    {
        if (string.IsNullOrWhiteSpace(NewLocation))
            return;

        if (Locations.Contains(NewLocation.Trim(), StringComparer.OrdinalIgnoreCase))
            return;

        var newLocations = new List<string>(Locations) { NewLocation.Trim() };
        await SavePreferencesAsync(IsEnabled, newLocations);
        NewLocation = string.Empty;
    }

    /// <summary>
    /// Removes a location from the preferences
    /// </summary>
    [RelayCommand]
    private async Task RemoveLocationAsync(string location)
    {
        var newLocations = Locations.Where(l => !l.Equals(location, StringComparison.OrdinalIgnoreCase)).ToList();
        await SavePreferencesAsync(IsEnabled, newLocations);
    }

    /// <summary>
    /// Toggles notifications enabled/disabled
    /// </summary>
    [RelayCommand]
    private async Task ToggleEnabledAsync()
    {
        await SavePreferencesAsync(IsEnabled, Locations);
    }

    /// <summary>
    /// Saves preferences to the API
    /// </summary>
    private async Task SavePreferencesAsync(bool isEnabled, List<string> locations)
    {
        try
        {
            IsBusy = true;
            var success = await _notificationService.UpdatePreferencesAsync(
                isEnabled,
                locations,
                IsPrivateSelected,
                IsBrokerSelected,
                IsPortalSelected);
            if (success)
            {
                _logger.LogInformation("Notification preferences saved successfully");
                await LoadPreferencesAsync();
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
