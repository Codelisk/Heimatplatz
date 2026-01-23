using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.UI.Xaml.Controls;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// HomePage - Hauptseite mit Immobilien-Liste
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// </summary>
public sealed partial class HomePage : BasePage
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;

    public HomePage()
    {
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Directly trigger page header setup when page loads
        ViewModel?.SetupPageHeader();
    }

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        Frame.Navigate(typeof(PropertyDetailPage), property.Id);
    }

    private async void OnPropertyBlocked(object sender, PropertyListItemDto property)
    {
        System.Diagnostics.Debug.WriteLine($"[HomePage] OnPropertyBlocked called for: {property.Title}");
        System.Diagnostics.Debug.WriteLine($"[HomePage] Properties count before remove: {ViewModel.Properties.Count}");

        // Remove from list immediately for responsive UX
        ViewModel.Properties.Remove(property);

        System.Diagnostics.Debug.WriteLine($"[HomePage] Properties count after remove: {ViewModel.Properties.Count}");

        // TODO: Call API to persist block
        // For now just remove from UI
        await Task.CompletedTask;
    }

    private void OnPropertyFavorited(object sender, PropertyListItemDto property)
    {
        System.Diagnostics.Debug.WriteLine($"[HomePage] Property favorited: {property.Title}");
        // TODO: Implement favorite functionality
        // For now just a visual feedback that it was triggered
    }
}
