using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

    private async void OnAddLocationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        if (!string.IsNullOrWhiteSpace(ViewModel.NewLocation))
        {
            await ViewModel.AddLocationCommand.ExecuteAsync(null);
        }
    }

    private async void OnRemoveLocationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || sender is not Button button || button.Tag is not string location)
            return;

        await ViewModel.RemoveLocationCommand.ExecuteAsync(location);
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

    private async void OnLocationKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            OnAddLocationClick(sender, e);
        }
    }
}
