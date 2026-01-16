using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// Debug-Overlay Control - Floating Button mit Schnellzugriff auf Debug-Funktionen
/// Nur in DEBUG-Builds verfuegbar
/// </summary>
public sealed partial class DebugOverlay : Page
{
    public DebugOverlayViewModel ViewModel { get; }

    public DebugOverlay(DebugOverlayViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        this.InitializeComponent();
    }
}
