using Heimatplatz.Features.Properties.Contracts.Models;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// FavoritesPage - Page for viewing favorited properties
/// Erbt von BasePage fuer automatisches INavigationAware Handling
/// </summary>
public sealed partial class FavoritesPage : BasePage
{
    public FavoritesPage()
    {
        this.InitializeComponent();
    }

    public FavoritesViewModel? ViewModel => DataContext as FavoritesViewModel;

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        ViewModel?.ViewPropertyDetailsCommand.Execute(property);
    }

    private void OnPropertyFavorited(object sender, PropertyListItemDto property)
    {
        // Auf der Favoriten-Seite bedeutet Klick = Entfernen
        ViewModel?.RemoveFromCollectionCommand.Execute(property);
    }
}
