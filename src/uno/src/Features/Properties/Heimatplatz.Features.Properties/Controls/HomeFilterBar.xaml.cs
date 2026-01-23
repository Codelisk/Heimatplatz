namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Filter-Leiste für den AppHeader (Desktop-Modus).
/// Wird via Uno Navigation in die HeaderCenter Region geladen.
/// DataContext (HomeFilterBarViewModel) wird automatisch via ViewMap gesetzt.
/// </summary>
public sealed partial class HomeFilterBar : UserControl
{
    public HomeFilterBar()
    {
        this.InitializeComponent();
    }
}
