using System.Collections.ObjectModel;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Maui.Controls;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class HomePage : ShinyContentPage
{
    private bool _chipBarHidden;
    private double? _chipBarAnimationTarget;
    private ToolbarItem? _filterToolbarItem;
    private HomeViewModel? _viewModel;

    /// <summary>
    /// Filter-Symbol rechts oben: erscheint nur, solange die Chip-Zeile weggescrollt
    /// ist, und holt sie per Tap zurueck. ToolbarItem kennt kein IsVisible, daher
    /// wird es dynamisch hinzugefuegt/entfernt. Text kommt aus dem Loc des ViewModels
    /// (Fallback DI-Resolve, falls das Item vor dem BindingContext erzeugt wird).
    /// </summary>
    private ToolbarItem FilterToolbarItem => _filterToolbarItem ??= new ToolbarItem
    {
        Text = (_viewModel?.Loc ?? IPlatformApplication.Current?.Services.GetRequiredService<HomeStringsLocalized>())?.FilterToolbar ?? string.Empty,
        IconImageSource = "icon_filter.png",
        Order = ToolbarItemOrder.Primary,
        AutomationId = "Home_Toolbar_Filter",
        Command = new Command(ShowChipBar)
    };

    public HomePage()
    {
        InitializeComponent();

        // Detents im Code-Behind ERSETZEN statt ergaenzen: XAML-Detents addieren zu den
        // Defaults (Quarter/Half/Full), wodurch Panels am kleinsten Detent oeffnen wuerden.
        SortPanel.Detents = new ObservableCollection<DetentValue> { new(0.62), DetentValue.Full };
        TypePanel.Detents = new ObservableCollection<DetentValue> { new(0.36), DetentValue.Half };
        AgePanel.Detents = new ObservableCollection<DetentValue> { new(0.42), DetentValue.Half };
        OrtPanel.Detents = new ObservableCollection<DetentValue> { new(0.75), DetentValue.Full };
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.ScrollToTopRequested -= OnScrollToTopRequested;

        _viewModel = BindingContext as HomeViewModel;
        if (_viewModel != null)
            _viewModel.ScrollToTopRequested += OnScrollToTopRequested;
    }

    /// <summary>
    /// Nach Reload/Seitenwechsel an den Listenanfang: die ItemsSource-Instanz bleibt
    /// gleich (ReplaceRange fuer den Recycling-Pool), die Scroll-Position wuerde den
    /// Inhaltstausch sonst ueberleben.
    /// </summary>
    private void OnScrollToTopRequested(object? sender, EventArgs e)
    {
#if WINDOWS
        // ScrollTo(0, Start) ankert Item 0 am Viewport-Anfang und schiebt den
        // Header-Spacer raus - die erste Kartenreihe laege hinter der Chip-Zeile.
        // Daher den inneren ScrollViewer auf echten Offset 0 (inkl. Header) setzen.
        if (PropertiesCollection.Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject platformView &&
            FindScrollViewer(platformView) is { } scrollViewer)
            scrollViewer.ChangeView(null, 0, null, disableAnimation: true);
#else
        if (_viewModel?.Properties.Count > 0)
            PropertiesCollection.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
#endif
        ShowChipBar();
    }

#if WINDOWS
    private static Microsoft.UI.Xaml.Controls.ScrollViewer? FindScrollViewer(Microsoft.UI.Xaml.DependencyObject root)
    {
        if (root is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
            return scrollViewer;

        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            if (FindScrollViewer(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i)) is { } found)
                return found;
        }

        return null;
    }
#endif

    /// <summary>
    /// Auto-Hide der Chip-Zeile: beim Runterscrollen ausblenden (voller Platz fuer
    /// Inhalte), beim Hochwischen oder am Listenanfang sofort wieder einblenden.
    /// </summary>
    private void OnPropertiesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // Am Listenanfang immer sichtbar. Schwelle deckt den Header-Spacer (56) ab:
        // beim initialen Laden/Seitenwechsel meldet WinUI einen 56px-Sprung
        // (Item 0 wird an den Viewport-Anfang geankert), das ist noch "oben".
        if (e.VerticalOffset <= 66)
        {
            ShowChipBar();
            return;
        }

        // Kleine Deltas ignorieren (Zittern beim langsamen Scrollen)
        if (e.VerticalDelta > 8)
            HideChipBar();
        else if (e.VerticalDelta < -8)
            ShowChipBar();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Selbstheilung: Wegnavigieren (z.B. Tap auf eine Karte) bricht eine laufende
        // Slide-Animation ab und laesst die Zeile sonst halb verschoben stehen -
        // auf iOS scheint sie dann verschwommen durch die transluzente Navbar und
        // ist nicht mehr klickbar. Beim Zurueckkehren den Sollzustand neu anfahren.
        TransitionChipBar(_chipBarHidden);
    }

    private void ShowChipBar() => TransitionChipBar(hide: false);

    private void HideChipBar() => TransitionChipBar(hide: true);

    /// <summary>
    /// Faehrt die Chip-Zeile in den gewuenschten Zustand. Kein Fire-and-Forget:
    /// der Endzustand wird nach der Animation hart gesetzt, ein gestrandeter
    /// Zwischenstand (extern abgebrochene Animation) beim naechsten Aufruf erneut
    /// animiert. Das Toolbar-Symbol wird erst nach abgeschlossenem Slide in die
    /// Navbar gehaengt - Add/Remove baut die native Leiste um, waehrend Scroll und
    /// Animation fuehrte das auf iOS zu eingefrorenen/unklickbaren Symbolen.
    /// </summary>
    private async void TransitionChipBar(bool hide)
    {
        // Host-Hoehe statt gemessener Hoehe: fix aus XAML (56) und damit auch vor
        // dem ersten Layout-Pass gueltig.
        var target = hide ? -(FilterChipBarHost.HeightRequest + 8) : 0d;

        // Kein Neustart, wenn das Ziel erreicht ist oder eine Animation dorthin
        // laeuft - Scrolled feuert am Listenanfang fuer jedes Delta erneut.
        if (_chipBarHidden == hide &&
            (_chipBarAnimationTarget == target ||
             (_chipBarAnimationTarget is null && FilterChipBar.TranslationY == target)))
            return;

        _chipBarHidden = hide;
        _chipBarAnimationTarget = target;

        if (!hide)
        {
            ToolbarItems.Remove(FilterToolbarItem);
            FilterChipBarHost.IsVisible = true;
        }

        var canceled = await FilterChipBar.TranslateToAsync(0, target, 160, hide ? Easing.CubicIn : Easing.CubicOut);

        if (_chipBarAnimationTarget == target)
            _chipBarAnimationTarget = null;

        // Abgebrochen oder Sollzustand inzwischen gewechselt: der Nachfolger (bzw.
        // die Selbstheilung beim naechsten Aufruf) uebernimmt.
        if (canceled || _chipBarHidden != hide)
            return;

        // Endzustand festnageln - Animationsreste (Bruchpixel) machen die Zeile auf
        // iOS unscharf.
        FilterChipBar.TranslationY = target;

        if (hide)
        {
            // WinUI clippt das Overlay nicht an der Navbar: ohne echtes Ausblenden
            // bleibt die weggeschobene Zeile ueber der Navbar sichtbar.
            FilterChipBarHost.IsVisible = false;

            if (!ToolbarItems.Contains(FilterToolbarItem))
                ToolbarItems.Add(FilterToolbarItem);
        }
    }
}
