using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Gemeinsamer App-Header fuer alle Seiten.
/// Enthaelt Logo und Auth-Bereich (Login/Register oder Profil-Menue).
/// DataContext (AppHeaderViewModel) wird automatisch via Uno Navigation ViewMap gesetzt.
/// Hamburger Button toggled NavigationPane via ToggleNavigationPaneEvent.
/// </summary>
public sealed partial class AppHeader : UserControl
{
    public AppHeader()
    {
        this.InitializeComponent();
    }
}
