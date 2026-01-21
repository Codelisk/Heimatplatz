using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// PropertyDetailPage - Detailansicht einer Immobilie
/// </summary>
public sealed partial class PropertyDetailPage : BasePage
{
    public PropertyDetailViewModel? ViewModel => DataContext as PropertyDetailViewModel;

    public PropertyDetailPage()
    {
        this.InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyBlocked += OnPropertyBlocked;
        }
    }

    private void OnPropertyBlocked(object? sender, EventArgs e)
    {
        // Navigate back after blocking - the property is now hidden from the list
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnImageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView flipView && ViewModel != null)
        {
            ViewModel.CurrentImageIndex = flipView.SelectedIndex + 1;
        }
    }

    private void OnThumbnailClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string imageUrl && ViewModel != null)
        {
            // Find the index of the clicked image
            var urls = ViewModel.Property?.ImageUrls;
            if (urls != null)
            {
                var index = urls.IndexOf(imageUrl);
                if (index >= 0)
                {
                    DesktopImageFlipView.SelectedIndex = index;
                }
            }
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

    private async void OnCopyLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string link && ViewModel != null)
        {
            await ViewModel.CopyToClipboardAsync(link);
        }
    }

}
