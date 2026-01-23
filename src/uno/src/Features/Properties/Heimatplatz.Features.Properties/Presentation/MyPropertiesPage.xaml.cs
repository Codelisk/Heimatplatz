using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// MyPropertiesPage - Page for managing user's own properties
/// Erbt von BasePage fuer automatisches INavigationAware Handling
/// </summary>
public sealed partial class MyPropertiesPage : BasePage
{
    public MyPropertiesPage()
    {
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;

    private void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Directly trigger page header setup when page loads
        ViewModel?.SetupPageHeader();
    }
}
