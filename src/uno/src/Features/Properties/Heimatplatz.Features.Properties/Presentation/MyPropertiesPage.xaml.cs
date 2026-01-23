using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// MyPropertiesPage - Page for managing user's own properties
/// Erbt von BasePage fuer automatisches INavigationAware Handling und PageNavigatedEvent
/// </summary>
public sealed partial class MyPropertiesPage : BasePage
{
    public MyPropertiesPage()
    {
        this.InitializeComponent();
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;
}
