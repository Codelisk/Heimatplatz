using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// HomePage - Hauptseite mit Immobilien-Liste
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// Page Header wird via INavigationAware.OnNavigatedTo in HomeViewModel gesetzt
/// </summary>
public sealed partial class HomePage : BasePage
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;

    public HomePage()
    {
        this.InitializeComponent();
    }

    private async void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        await this.Navigator()!.NavigateRouteAsync(this, "PropertyDetail", data: property.Id);
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
