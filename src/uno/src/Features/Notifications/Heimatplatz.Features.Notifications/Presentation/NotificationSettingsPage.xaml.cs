using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Notifications.Presentation;

/// <summary>
/// Notification settings page
/// Inherits from BasePage for automatic header handling
/// </summary>
public sealed partial class NotificationSettingsPage : BasePage
{
    public NotificationSettingsViewModel? ViewModel => DataContext as NotificationSettingsViewModel;

    public NotificationSettingsPage()
    {
        this.InitializeComponent();
    }

    private async void OnNotificationsToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || sender is not ToggleSwitch toggle)
            return;

        await ViewModel.ToggleEnabledCommand.ExecuteAsync(null);
    }

    private async void OnCustomFilterChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        await ViewModel.SavePreferencesCommand.ExecuteAsync(null);
    }
}
