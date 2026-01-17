using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite zum Bearbeiten einer bestehenden Immobilie
/// </summary>
public sealed partial class EditPropertyPage : Page
{
    public EditPropertyViewModel ViewModel => (EditPropertyViewModel)DataContext;

    public EditPropertyPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            // Try to extract PropertyId from navigation parameter
            IDictionary<string, object>? data = e.Parameter as IDictionary<string, object>;

            // If parameter is not a dictionary, try to create one from the raw parameter
            if (data == null && e.Parameter != null)
            {
                // Handle case where parameter might be passed differently
                data = new Dictionary<string, object>();
                if (e.Parameter is Guid guid)
                {
                    data["PropertyId"] = guid;
                }
            }

            if (data != null)
            {
                await ViewModel.OnNavigatedToAsync(data);
            }
        }
    }
}
