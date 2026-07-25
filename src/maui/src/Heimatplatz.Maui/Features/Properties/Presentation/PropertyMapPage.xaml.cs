namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class PropertyMapPage : ContentPage
{
    public PropertyMapPage()
    {
        InitializeComponent();
    }

    private PropertyMapViewModel? Vm => BindingContext as PropertyMapViewModel;

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Mini-Zettel-CTA -> native Detailseite statt Web-Detailseite
        if (Vm?.TryHandleListingLink(e.Url) == true)
            e.Cancel = true;
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        => Vm?.OnWebNavigated(e.Result);
}
