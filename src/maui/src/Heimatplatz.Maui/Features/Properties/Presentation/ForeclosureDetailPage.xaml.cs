namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class ForeclosureDetailPage : ContentPage
{
    public ForeclosureDetailPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Zurueck schliesst zuerst die offene Lightbox statt die Seite zu verlassen
    /// (Android-Hardware-Back und Shell-Zurueck-Button).
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is ForeclosureDetailViewModel { IsImageViewerOpen: true } vm)
        {
            vm.IsImageViewerOpen = false;
            return true;
        }

        return base.OnBackButtonPressed();
    }

    /// <summary>
    /// Haelt auf Android die intern gewechselte Carousel-Position und den gebundenen
    /// Bildzaehler synchron.
    /// </summary>
    private void OnImagePositionChanged(object? sender, PositionChangedEventArgs e)
    {
        if (BindingContext is ForeclosureDetailViewModel vm && vm.CurrentImagePosition != e.CurrentPosition)
            vm.CurrentImagePosition = e.CurrentPosition;
    }
}
