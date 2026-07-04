using Heimatplatz.Maui.Features.Properties;
using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    public AppShell()
    {
        InitializeComponent();

        // Standardweg fuer die Inseratserstellung: KI-Flow auf Android/iOS-Phones,
        // manuelle Erfassung auf allen anderen Geraeten
        Navigate.SetRoute(AddPropertyMenuItem, PropertyCreationRoutes.Default);
    }
}
