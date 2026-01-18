using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Heimatplatz.Features.Notifications.Presentation;

/// <summary>
/// Notification settings page
/// </summary>
public sealed partial class NotificationSettingsPage : Page
{
    public NotificationSettingsViewModel? ViewModel { get; set; }

    public NotificationSettingsPage()
    {
        this.InitializeComponent();
    }

    private async void OnAddLocationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        var location = await ViewModel.NewLocation;
        if (!string.IsNullOrWhiteSpace(location))
        {
            await ViewModel.AddLocation(location);
            await ViewModel.NewLocation.UpdateAsync(_ => string.Empty, CancellationToken.None);
            LocationTextBox.Focus(FocusState.Programmatic);
        }
    }

    private async void OnRemoveLocationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || sender is not Button button || button.Tag is not string location)
            return;

        await ViewModel.RemoveLocation(location);
    }

    private async void OnNotificationsToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || sender is not ToggleSwitch toggle)
            return;

        await ViewModel.ToggleEnabled(toggle.IsOn);
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
