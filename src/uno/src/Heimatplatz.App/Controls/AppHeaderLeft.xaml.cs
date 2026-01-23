using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Left part of the AppHeader containing Hamburger/Back button and Logo/Title.
/// Shares AppHeaderViewModel with AppHeaderRight for consistent state.
/// DataContext (AppHeaderViewModel) is set automatically via Uno Navigation ViewMap.
/// </summary>
public sealed partial class AppHeaderLeft : UserControl
{
    public AppHeaderLeft()
    {
        this.InitializeComponent();
    }
}
