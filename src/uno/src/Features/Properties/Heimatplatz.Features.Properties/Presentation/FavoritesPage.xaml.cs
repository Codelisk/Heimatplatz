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
}
