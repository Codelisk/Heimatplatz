using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Right part of the AppHeader containing Auth buttons (Login/Register or User Profile).
/// Shares AppHeaderViewModel with AppHeaderLeft for consistent state.
/// DataContext (AppHeaderViewModel) is set automatically via Uno Navigation ViewMap.
/// </summary>
public sealed partial class AppHeaderRight : UserControl
{
    public AppHeaderRight()
    {
        this.InitializeComponent();
    }
}
