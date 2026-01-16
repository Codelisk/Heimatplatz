using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// HomePage - Hauptseite mit Immobilien-Liste
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
    }

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        Frame.Navigate(typeof(PropertyDetailPage), property.Id);
    }
}
