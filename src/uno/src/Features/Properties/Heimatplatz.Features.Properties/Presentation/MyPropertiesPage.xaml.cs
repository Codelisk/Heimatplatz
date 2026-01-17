using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// MyPropertiesPage - Page for managing user's own properties
/// </summary>
public sealed partial class MyPropertiesPage : Page
{
    public MyPropertiesPage()
    {
        this.InitializeComponent();
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync();
        }
    }
}
