using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// MyPropertiesPage - Page for managing user's own properties
/// </summary>
public sealed partial class MyPropertiesPage : Page
{
    private bool _isLoaded = false;

    public MyPropertiesPage()
    {
        this.InitializeComponent();
        this.Loaded += MyPropertiesPage_Loaded;
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;

    private async void MyPropertiesPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Only load once to avoid reloading when navigating back
        if (!_isLoaded && ViewModel != null)
        {
            _isLoaded = true;
            await ViewModel.OnNavigatedToAsync();
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync();
        }
    }
}
