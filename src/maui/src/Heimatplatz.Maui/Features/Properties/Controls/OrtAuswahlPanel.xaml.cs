using System.Collections.ObjectModel;
using Heimatplatz.Maui.Controls;
using Heimatplatz.Maui.Features.Properties.Presentation;
using Shiny.Maui.Controls;

namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// Gemeinsames Ort-Auswahl-Panel. Seiten setzen den BindingContext auf ihr
/// <see cref="OrtAuswahlViewModel"/> und haengen das Panel in ihren OverlayHost.
/// </summary>
public partial class OrtAuswahlPanel : ReliableFloatingPanel
{
    private OrtAuswahlViewModel? _viewModel;

    public OrtAuswahlPanel()
    {
        InitializeComponent();

        // Detents im Code-Behind ERSETZEN statt ergaenzen: XAML-Detents addieren zu den
        // Defaults (Quarter/Half/Full), das Panel wuerde sonst am kleinsten Detent oeffnen.
        // Fester Anteil statt FitContent: die Liste ist beliebig lang und scrollt selbst.
        Detents = new ObservableCollection<DetentValue> { new(0.75), DetentValue.Full };
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.ScrollToBezirkRequested -= OnScrollToBezirkRequested;

        _viewModel = BindingContext as OrtAuswahlViewModel;
        if (_viewModel != null)
            _viewModel.ScrollToBezirkRequested += OnScrollToBezirkRequested;
    }

    /// <summary>
    /// Akkordeon-Begleiter: der frisch aufgeklappte Bezirk wandert an den Listenanfang,
    /// sonst oeffnet sich bei einem Bezirk weit unten die Gemeindeliste ausserhalb des
    /// Sichtbereichs. Die Kinder des BindableLayouts stehen in derselben Reihenfolge
    /// wie OrtBezirke, der Index passt also 1:1.
    /// </summary>
    private async void OnScrollToBezirkRequested(object? sender, int index)
    {
        // Erst messen lassen: unmittelbar nach dem Aufklappen liefert das Layout
        // noch die alten Positionen.
        await Task.Delay(80);

        if (index < 0 || index >= OrtBezirkeStack.Children.Count)
            return;

        if (OrtBezirkeStack.Children[index] is Element target)
            await OrtBrowseScroll.ScrollToAsync(target, ScrollToPosition.Start, animated: true);
    }
}
