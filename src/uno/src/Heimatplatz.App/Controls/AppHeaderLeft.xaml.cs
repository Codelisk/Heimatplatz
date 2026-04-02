using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Left part of the AppHeader containing Hamburger/Back button and Logo/Title.
/// DataContext (AppHeaderLeftViewModel) is set by MainPage via DI.
/// </summary>
public sealed partial class AppHeaderLeft : UserControl
{
    public AppHeaderLeft()
    {
        this.InitializeComponent();
    }
}
