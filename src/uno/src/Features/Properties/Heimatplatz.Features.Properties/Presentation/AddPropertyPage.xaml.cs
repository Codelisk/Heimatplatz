using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite zum Hinzufügen einer neuen Immobilie
/// Erbt von BasePage fuer automatisches INavigationAware Handling und PageNavigatedEvent
/// </summary>
public sealed partial class AddPropertyPage : BasePage
{
    public AddPropertyViewModel? ViewModel => DataContext as AddPropertyViewModel;

    public AddPropertyPage()
    {
        InitializeComponent();
    }
}
