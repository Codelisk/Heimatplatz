using Heimatplatz.Features.Properties.Contracts.Models;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Page for displaying and managing user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// Erbt von BasePage fuer automatisches INavigationAware Handling.
/// </summary>
public sealed partial class BlockedPage : BasePage
{
    public BlockedPage()
    {
        this.InitializeComponent();
    }

    public BlockedViewModel? ViewModel => DataContext as BlockedViewModel;

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        ViewModel?.ViewPropertyDetailsCommand.Execute(property);
    }

    private void OnPropertyBlocked(object sender, PropertyListItemDto property)
    {
        // Auf der Blockiert-Seite bedeutet Klick = Entblockieren
        ViewModel?.RemoveFromCollectionCommand.Execute(property);
    }
}
