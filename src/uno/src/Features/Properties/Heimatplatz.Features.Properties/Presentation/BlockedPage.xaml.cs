using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Page for displaying and managing user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// Supports bulk unblock via selection mode.
/// </summary>
public sealed partial class BlockedPage : Page
{
    public BlockedPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    public BlockedViewModel? ViewModel => DataContext as BlockedViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SetupPageHeader();
            await ViewModel.OnNavigatedToAsync();
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            ViewModel.SetupPageHeader();
            await ViewModel.OnNavigatedToAsync();
        }
    }

    private void OnPropertyCardTapped(object sender, TappedRoutedEventArgs e)
    {
        // In selection mode, tapping the card toggles selection
        if (ViewModel?.IsSelectionMode == true && sender is FrameworkElement element)
        {
            if (element.DataContext is PropertyListItemDto property)
            {
                ViewModel.TogglePropertySelectionCommand.Execute(property);

                // Update visual state of the selection button
                UpdateSelectionVisual(element, property);
                e.Handled = true;
            }
        }
    }

    private void UpdateSelectionVisual(FrameworkElement container, PropertyListItemDto property)
    {
        // Find the selection button in the container and update its visual
        var selectionButton = FindChildOfType<Button>(container);
        if (selectionButton != null && ViewModel != null)
        {
            var isSelected = ViewModel.IsPropertySelected(property);
            var icon = selectionButton.Content as FontIcon;
            if (icon != null)
            {
                // Show filled check when selected, empty circle when not
                icon.Glyph = isSelected ? "\uE73E" : "\uE739"; // Checkmark vs Circle
                icon.Foreground = isSelected
                    ? (Brush)Application.Current.Resources["AccentBrush"]
                    : (Brush)Application.Current.Resources["OnSurfaceVariantBrush"];
            }

            // Update button background
            selectionButton.Background = isSelected
                ? (Brush)Application.Current.Resources["AccentBrush"]
                : (Brush)Application.Current.Resources["SurfaceBrush"];
        }
    }

    private static T? FindChildOfType<T>(DependencyObject parent) where T : class
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindChildOfType<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}
