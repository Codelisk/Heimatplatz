using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// FavoritesPage - Page for viewing favorited properties
/// </summary>
public sealed partial class FavoritesPage : Page
{
    public FavoritesPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    public FavoritesViewModel? ViewModel => DataContext as FavoritesViewModel;

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
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
