using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// Debug-Start-Page - Nur in DEBUG-Builds verfuegbar
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// </summary>
public sealed partial class DebugStartPage : Page
{
    public DebugStartViewModel ViewModel => (DebugStartViewModel)DataContext;

    public DebugStartPage()
    {
        this.InitializeComponent();
    }
}
