using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// HomePage - Hauptseite mit Immobilien-Liste
/// </summary>
public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = new HomeViewModel();
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        Frame.Navigate(typeof(PropertyDetailPage), property.Id);
    }
}
