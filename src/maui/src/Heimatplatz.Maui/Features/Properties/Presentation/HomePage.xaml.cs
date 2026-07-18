using System.Collections.ObjectModel;
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
    /// wird es dynamisch hinzugefuegt/entfernt.
    /// </summary>
    private ToolbarItem FilterToolbarItem => _filterToolbarItem ??= new ToolbarItem
    {
        Text = "Filter",
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
        if (_viewModel?.Properties.Count > 0)
            PropertiesCollection.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
        ShowChipBar();
    }

    /// <summary>
    /// Auto-Hide der Chip-Zeile: beim Runterscrollen ausblenden (voller Platz fuer
    /// Inhalte), beim Hochwischen oder am Listenanfang sofort wieder einblenden.
    /// </summary>
    private void OnPropertiesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // Am Listenanfang immer sichtbar
        if (e.VerticalOffset <= 10)
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
        _ = FilterChipBar.TranslateToAsync(0, 0, 160, Easing.CubicOut);
    }

    private void HideChipBar()
    {
        if (_chipBarHidden) return;
        _chipBarHidden = true;
        if (!ToolbarItems.Contains(FilterToolbarItem))
            ToolbarItems.Add(FilterToolbarItem);
        _ = FilterChipBar.TranslateToAsync(0, -(FilterChipBar.Height + 8), 160, Easing.CubicIn);
    }
}
