using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

        // Standardweg fuer die Inseratserstellung: KI-Flow auf Android/iOS-Phones
        Navigate.SetRoute(AddPropertyToolbarItem, PropertyCreationRoutes.Default);
    }
}
