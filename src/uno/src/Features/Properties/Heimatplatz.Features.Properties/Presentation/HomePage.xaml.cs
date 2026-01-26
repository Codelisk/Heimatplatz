using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Application;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// HomePage - Hauptseite mit Immobilien-Liste
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// Page Header wird via INavigationAware.OnNavigatedTo in HomeViewModel gesetzt
/// </summary>
public sealed partial class HomePage : BasePage
{
    private IPropertyStatusService? _propertyStatusService;

    public HomeViewModel ViewModel => (HomeViewModel)DataContext;

    private IPropertyStatusService PropertyStatusService =>
        _propertyStatusService ??= ((IApplicationWithServices)Application.Current).Services!.GetRequiredService<IPropertyStatusService>();

    public HomePage()
    {
        this.InitializeComponent();
    }

    private async void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        // Navigate to ForeclosureDetail for foreclosure properties, PropertyDetail otherwise
        if (property.Type == PropertyType.Foreclosure)
        {
            await this.Navigator()!.NavigateRouteAsync(this, "ForeclosureDetail", data: new ForeclosureDetailData(property.Id));
        }
        else
        {
            await this.Navigator()!.NavigateRouteAsync(this, "PropertyDetail", data: new PropertyDetailData(property.Id));
        }
    }

    private async void OnPropertyBlocked(object sender, PropertyListItemDto property)
    {
        System.Diagnostics.Debug.WriteLine($"[HomePage] OnPropertyBlocked called for: {property.Title}");

        var isBlocked = await PropertyStatusService.ToggleBlockedAsync(property.Id);

        if (isBlocked)
        {
            // Property was blocked - remove from list
            System.Diagnostics.Debug.WriteLine($"[HomePage] Property blocked - removing from list");
            ViewModel.Properties.Remove(property);
        }
        else
        {
            // Property was unblocked - update card status if available
            System.Diagnostics.Debug.WriteLine($"[HomePage] Property unblocked");
            if (sender is PropertyCard card)
            {
                card.IsBlocked = false;
            }
        }
    }

    private async void OnPropertyFavorited(object sender, PropertyListItemDto property)
    {
        System.Diagnostics.Debug.WriteLine($"[HomePage] OnPropertyFavorited called for: {property.Title}");

        var isFavorite = await PropertyStatusService.ToggleFavoriteAsync(property.Id);

        System.Diagnostics.Debug.WriteLine($"[HomePage] Property is now favorite: {isFavorite}");

        // Update card status
        if (sender is PropertyCard card)
        {
            card.IsFavorite = isFavorite;
        }
    }

    // NOTE: OnPropertyCardLoaded was intentionally removed.
    // Setting DependencyProperties (IsFavorite/IsBlocked) on PropertyCard during
    // ItemsRepeater rendering triggered a synchronous recursive cascade through
    // Uno's DependencyObjectStore → BindingExpression → OnValueChanged → StackOverflow.
    // Favorite/blocked status is now loaded on-demand in PropertyCard.OnMenuFlyoutOpening
    // via RefreshStatusFromService().
}
