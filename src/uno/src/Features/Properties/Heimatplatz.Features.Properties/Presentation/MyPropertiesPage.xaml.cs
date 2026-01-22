using Microsoft.Extensions.Logging;
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
        this.DataContextChanged += OnDataContextChanged;
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;

    private async void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.SetupPageHeader();
            await ViewModel.OnNavigatedToAsync();
        }
    }

}
