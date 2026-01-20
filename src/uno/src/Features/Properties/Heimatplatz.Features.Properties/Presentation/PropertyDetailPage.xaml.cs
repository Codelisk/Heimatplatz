using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// PropertyDetailPage - Detailansicht einer Immobilie
/// </summary>
public sealed partial class PropertyDetailPage : Page
{
    public PropertyDetailViewModel ViewModel { get; }

    public PropertyDetailPage()
    {
        ViewModel = new PropertyDetailViewModel();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Parameter kann Guid oder string sein
        Guid? propertyId = e.Parameter switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null
        };

        if (propertyId.HasValue)
        {
            ViewModel.LoadProperty(propertyId.Value);
        }
        else
        {
            // Fallback: Lade Testdaten
            ViewModel.LoadProperty(Guid.NewGuid());
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnContactClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ContactSeller();
    }

    private void OnImageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView flipView)
        {
            ViewModel.CurrentImageIndex = flipView.SelectedIndex + 1;
        }
    }

    private void OnThumbnailClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string imageUrl)
        {
            // Find the index of the clicked image
            var urls = ViewModel.Property?.ImageUrls;
            if (urls != null)
            {
                var index = urls.IndexOf(imageUrl);
                if (index >= 0)
                {
                    DesktopImageFlipView.SelectedIndex = index;
                }
            }
        }
    }
}
