using System.Collections.ObjectModel;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Maui.Controls;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class HomePage : ShinyContentPage
{
    private bool _chipBarHidden;
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

    private void ShowChipBar()
    {
        if (!_chipBarHidden) return;
        _chipBarHidden = false;
        ToolbarItems.Remove(FilterToolbarItem);
        FilterChipBarHost.IsVisible = true;
        _ = FilterChipBar.TranslateToAsync(0, 0, 160, Easing.CubicOut);
    }

    private async void HideChipBar()
    {
        if (_chipBarHidden) return;
        _chipBarHidden = true;
        if (!ToolbarItems.Contains(FilterToolbarItem))
            ToolbarItems.Add(FilterToolbarItem);

        // WinUI clippt das Overlay nicht an der Navbar: ohne echtes Ausblenden bleibt
        // die weggeschobene Zeile ueber der Navbar sichtbar. Nach dem Slide daher den
        // Host verstecken - ausser ein zwischenzeitliches ShowChipBar hat die Animation
        // abgebrochen (canceled) oder den Zustand schon zurueckgesetzt.
        var canceled = await FilterChipBar.TranslateToAsync(0, -(FilterChipBar.Height + 8), 160, Easing.CubicIn);
        if (!canceled && _chipBarHidden)
            FilterChipBarHost.IsVisible = false;
    }
}
