using System.Collections.ObjectModel;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Maui.Controls;
#if IOS
using Microsoft.Maui.Platform;
#endif

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class HomePage : ShinyContentPage
{
    /// <summary>
    /// Weg der Karte-Pille beim Ausblenden: Hoehe (40) + Aussenabstand (20) aus
    /// dem XAML, plus Reserve fuer die Kontur. Zusaetzlich wird ausgeblendet -
    /// reines Verschieben reicht nicht, weil das Seiten-Overlay nicht auf jeder
    /// Plattform an der Unterkante clippt.
    /// </summary>
    private const double MapPillHiddenOffset = 76;

    private bool _chipBarHidden;
    private double? _chipBarAnimationTarget;
    private ToolbarItem? _filterToolbarItem;
    private HomeViewModel? _viewModel;
#if IOS
    private bool _iosThemeChangeSubscribed;
#endif

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

        // Die drei kurzen Auswahl-Sheets bemessen sich am Inhalt statt an einem festen
        // Anteil der Bildschirmhoehe. Feste Anteile waren auf 800-dp-Geraeten zu knapp:
        // das Typ-Sheet brauchte ~556 px, bekam bei 0.36 aber nur ~455 px - "Fertig" war
        // beim Oeffnen abgeschnitten und erst nach Scrollen bedienbar (dasselbe traf die
        // letzte Sortier-Option). FitContent misst den Inhalt und rechnet den Detent
        // selbst aus, bleibt also unabhaengig von Geraetegroesse und Schriftskalierung.
        SortPanel.FitContent = true;
        TypePanel.FitContent = true;
        AgePanel.FitContent = true;

        // Das Ort-Panel bleibt bei einem festen Anteil: seine Liste ist beliebig lang
        // und scrollt selbst - am Inhalt bemessen waere es immer bildschirmfuellend.
        // Detents im Code-Behind ERSETZEN statt ergaenzen: XAML-Detents addieren zu den
        // Defaults (Quarter/Half/Full), wodurch Panels am kleinsten Detent oeffnen wuerden.
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
#elif ANDROID
        // Gleiches Problem wie unter WinUI: ScrollTo(0, Start) ankert die erste KARTE
        // am Viewport-Anfang, der Header-Spacer (56px) faellt raus und die Karte
        // beginnt hinter der Chip-Zeile. Adapter-Position 0 ist der Header - dorthin
        // springen nimmt den Spacer mit.
        if (PropertiesCollection.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
            recyclerView.ScrollToPosition(0);
        else if (_viewModel?.Properties.Count > 0)
            PropertiesCollection.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
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

#if IOS
        if (!_iosThemeChangeSubscribed && Application.Current is { } app)
        {
            app.RequestedThemeChanged += OnIosRequestedThemeChanged;
            _iosThemeChangeSubscribed = true;
        }

        // Sofort fuer bereits erzeugte Handler und nochmals nach dem aktuellen
        // Layout-Pass: der MAUI-10-Handler realisiert den Header erst als native
        // Supplementary View und das UIRefreshControl wird in die CollectionView
        // eingesetzt. Beide sollen explizit dem APP- statt dem Geraete-Theme folgen.
        ApplyIosScrollSurfaceTheme();
        Dispatcher.Dispatch(ApplyIosScrollSurfaceTheme);
#endif

        // Selbstheilung: Wegnavigieren (z.B. Tap auf eine Karte) bricht eine laufende
        // Slide-Animation ab und laesst die Zeile sonst halb verschoben stehen -
        // auf iOS scheint sie dann verschwommen durch die transluzente Navbar und
        // ist nicht mehr klickbar. Beim Zurueckkehren den Sollzustand neu anfahren.
        TransitionChipBar(_chipBarHidden);
    }

    protected override void OnDisappearing()
    {
#if IOS
        if (_iosThemeChangeSubscribed && Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnIosRequestedThemeChanged;
            _iosThemeChangeSubscribed = false;
        }
#endif

        base.OnDisappearing();
    }

#if IOS
    private void OnIosRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        Dispatcher.Dispatch(ApplyIosScrollSurfaceTheme);

    private void ApplyIosScrollSurfaceTheme()
    {
        if (PropertiesCollection.Handler?.PlatformView is not UIKit.UIView platformView ||
            FindCollectionView(platformView) is not { } collectionView)
            return;

        if (Application.Current is not { } app)
            return;

        // Konkrete Markenfarbe aus demselben ResourceDictionary wie die XAML-
        // Bindings, aber explizit anhand des effektiven APP-Themes ausgewaehlt.
        // Dadurch kann weder das Geraete-Theme noch ein beim Start gespeicherter
        // UIKit-Trait den Bounce-/Pull-to-Refresh-Bereich invertieren.
        var resourceKey = app.RequestedTheme == AppTheme.Dark ? "OffBlack" : "Paper";
        if (!app.Resources.TryGetValue(resourceKey, out var resource) || resource is not Color color)
            return;

        var background = color.ToPlatform();

        collectionView.BackgroundColor = background;

        if (collectionView.RefreshControl is { } refreshControl)
            refreshControl.BackgroundColor = background;

        // Falls MAUI das UIRefreshControl fuer diese Navbar-Konfiguration per
        // InsertSubview statt ueber UIScrollView.RefreshControl einsetzt.
        ApplyRefreshControlBackground(collectionView, background);

        // Der optimierte MAUI-10-CollectionView-Handler rendert den globalen Header
        // als wiederverwendbare Supplementary View mit transparentem ContentView.
        // Auch deren native Rueckseite explizit deckend halten; die BoxView im XAML
        // bleibt zusaetzlich die eigentliche sichtbare Flaeche.
        foreach (var header in collectionView.GetVisibleSupplementaryViews(
                     UIKit.UICollectionElementKindSectionKey.Header))
        {
            header.BackgroundColor = background;
            if (header is UIKit.UICollectionViewCell cell)
                cell.ContentView.BackgroundColor = background;
        }

        collectionView.SetNeedsDisplay();
    }

    private static UIKit.UICollectionView? FindCollectionView(UIKit.UIView view)
    {
        if (view is UIKit.UICollectionView collectionView)
            return collectionView;

        foreach (var child in view.Subviews)
        {
            if (FindCollectionView(child) is { } found)
                return found;
        }

        return null;
    }

    private static void ApplyRefreshControlBackground(UIKit.UIView view, UIKit.UIColor background)
    {
        foreach (var child in view.Subviews)
        {
            if (child is UIKit.UIRefreshControl refreshControl)
                refreshControl.BackgroundColor = background;

            ApplyRefreshControlBackground(child, background);
        }
    }
#endif

    private void ShowChipBar() => TransitionChipBar(hide: false);

    private void HideChipBar() => TransitionChipBar(hide: true);

    /// <summary>
    /// Endzustand der Karte-Pille ohne Animation setzen. IsVisible ist dabei
    /// load-bearing: eine nur durchsichtige Pille faengt weiterhin Taps ab und
    /// wuerde am unteren Listenrand ins Leere fuehren.
    /// </summary>
    private void SnapMapPill(bool hide)
    {
        MapPill.TranslationY = hide ? MapPillHiddenOffset : 0d;
        MapPill.Opacity = hide ? 0 : 1;
        MapPill.IsVisible = !hide;
    }

    private void OnFilterChipBarMetricsChanged(object? sender, EventArgs e) =>
        Dispatcher.Dispatch(UpdateFilterOverflowHint);

    private void OnFilterChipBarScrolled(object? sender, ScrolledEventArgs e) =>
        UpdateFilterOverflowHint();

    private async void OnFilterOverflowHintTapped(object? sender, TappedEventArgs e)
    {
        var maximumOffset = Math.Max(0, FilterChipContent.Width - FilterChipBar.Width);
        await FilterChipBar.ScrollToAsync(maximumOffset, 0, animated: true);
        UpdateFilterOverflowHint();
    }

    private void UpdateFilterOverflowHint()
    {
        var maximumOffset = Math.Max(0, FilterChipContent.Width - FilterChipBar.Width);
        FilterOverflowHint.IsVisible = maximumOffset > 8 &&
                                       FilterChipBar.ScrollX < maximumOffset - 4;
    }

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
             (_chipBarAnimationTarget is null && FilterChipBarSurface.TranslationY == target)))
        {
            // Selbstheilung nur im Ruhezustand: eine abgebrochene Animation
            // (Wegnavigieren) kann die Pille halb verschoben zurueckgelassen
            // haben. Waehrend einer laufenden Animation NICHT eingreifen.
            if (_chipBarAnimationTarget is null)
                SnapMapPill(hide);
            return;
        }

        _chipBarHidden = hide;
        _chipBarAnimationTarget = target;

        if (!hide)
        {
            ToolbarItems.Remove(FilterToolbarItem);
            FilterChipBarHost.IsVisible = true;
            MapPill.IsVisible = true;
        }

        // Pille laeuft parallel und in die Gegenrichtung (nach unten aus dem Bild)
        var pillTask = MapPill.TranslateToAsync(0, hide ? MapPillHiddenOffset : 0d, 160, hide ? Easing.CubicIn : Easing.CubicOut);
        var pillFadeTask = MapPill.FadeToAsync(hide ? 0 : 1, 160);

        var canceled = await FilterChipBarSurface.TranslateToAsync(0, target, 160, hide ? Easing.CubicIn : Easing.CubicOut);
        await Task.WhenAll(pillTask, pillFadeTask);

        if (_chipBarAnimationTarget == target)
            _chipBarAnimationTarget = null;

        // Abgebrochen oder Sollzustand inzwischen gewechselt: der Nachfolger (bzw.
        // die Selbstheilung beim naechsten Aufruf) uebernimmt.
        if (canceled || _chipBarHidden != hide)
            return;

        // Endzustand festnageln - Animationsreste (Bruchpixel) machen die Zeile auf
        // iOS unscharf.
        FilterChipBarSurface.TranslationY = target;
        SnapMapPill(hide);

        if (hide)
        {
#if WINDOWS
            // NUR WinUI: dort clippt das Overlay nicht an der Navbar, ohne echtes
            // Ausblenden bleibt die weggeschobene Zeile ueber der Navbar sichtbar.
            // Auf iOS darf der Host NICHT unsichtbar werden: ein IsVisible-Toggle
            // laesst die ScrollView beim Wiedereinblenden stale rendern (unscharf,
            // Taps gehen ins Leere, bis man sie seitlich scrollt und damit ein
            // Re-Layout erzwingt). Dort reicht IsClippedToBounds des Hosts.
            FilterChipBarHost.IsVisible = false;
#endif

            if (!ToolbarItems.Contains(FilterToolbarItem))
                ToolbarItems.Add(FilterToolbarItem);
        }
    }
}
