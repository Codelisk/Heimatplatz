using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite zum Hinzufügen einer neuen Immobilie
/// </summary>
public sealed partial class AddPropertyPage : Page
{
    public AddPropertyViewModel ViewModel => (AddPropertyViewModel)DataContext;

    public AddPropertyPage()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // Navigation zurück zur HomePage
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
