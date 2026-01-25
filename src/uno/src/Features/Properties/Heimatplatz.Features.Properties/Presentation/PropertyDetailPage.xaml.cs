using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// PropertyDetailPage - Detailansicht einer Immobilie (Immoscout Style)
/// </summary>
public sealed partial class PropertyDetailPage : BasePage
{
    public PropertyDetailViewModel? ViewModel => DataContext as PropertyDetailViewModel;

    public PropertyDetailPage()
    {
        this.InitializeComponent();
    }

    private async void OnBackClick(object sender, RoutedEventArgs e)
    {
        await this.Navigator()!.NavigateBackAsync(this);
    }

    private void OnImageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView flipView && ViewModel != null)
        {
            ViewModel.CurrentImageIndex = flipView.SelectedIndex + 1;
        }
    }

    private async void OnCopyEmailClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string email && ViewModel != null)
        {
            await ViewModel.CopyToClipboardAsync(email);
        }
    }

    private async void OnCopyPhoneClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string phone && ViewModel != null)
        {
            await ViewModel.CopyToClipboardAsync(phone);
        }
    }
}
