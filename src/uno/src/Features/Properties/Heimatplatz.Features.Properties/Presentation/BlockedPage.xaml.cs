using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Page for displaying and managing user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// </summary>
public sealed partial class BlockedPage : Page
{
    public BlockedPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    public BlockedViewModel? ViewModel => DataContext as BlockedViewModel;

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
