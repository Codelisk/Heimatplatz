using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// Debug-Start-Page - Nur in DEBUG-Builds verfuegbar
/// ViewModel wird via Uno.Extensions.Navigation automatisch injiziert
/// </summary>
public sealed partial class DebugStartPage : Page
{
    public DebugStartViewModel? ViewModel { get; private set; }

    public DebugStartPage()
    {
        this.DataContextChanged += OnDataContextChanged;
        this.InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = args.NewValue as DebugStartViewModel;
        Bindings.Update();
    }
}
