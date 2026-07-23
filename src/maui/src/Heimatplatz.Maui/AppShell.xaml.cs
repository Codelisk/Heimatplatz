using Heimatplatz.Maui.Localization;
using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    readonly List<FlyoutMenuEntry> flyoutEntries = [];
    Window? observedWindow;

    public AppShellStringsLocalized Loc { get; }

    public AppShell(AppShellStringsLocalized loc)
    {
        Loc = loc;
        // Shell ist ihr eigener BindingContext: XAML bindet Titel/Links auf Loc.*
        BindingContext = this;
        InitializeComponent();

#if DEBUG
        // Debug-Werkzeuge (z.B. API-Umschalter) nur in Entwicklungs-Builds im Flyout
        Items.Add(new ShellContent
        {
            Title = Loc.DebugTitle,
            Icon = "icon_bug.png",
            Route = "Debug",
            ContentTemplate = new DataTemplate(typeof(Features.Debug.Presentation.DebugPage))
        });
#endif

        BuildFlyoutEntries();

        VersionLabel.Text = Loc.VersionFormat(AppInfo.Current.VersionString);

        // Tablet bleibt beim ueberlagernden Drawer. Erst auf wirklich breiten
        // Desktop-Fenstern wird die Navigation dauerhaft sichtbar; Phones bleiben
        // unabhaengig von ihrer Ausrichtung beim bisherigen Flyout.
        SizeChanged += OnShellSizeChanged;
        Loaded += OnShellLoaded;
        Unloaded += OnShellUnloaded;
    }

    private void OnShellLoaded(object? sender, EventArgs e)
    {
        if (observedWindow == Window)
        {
            ApplyFlyoutBehavior();
            return;
        }

        if (observedWindow != null)
            observedWindow.PropertyChanged -= OnWindowPropertyChanged;

        observedWindow = Window;
        if (observedWindow != null)
            observedWindow.PropertyChanged += OnWindowPropertyChanged;

        ApplyFlyoutBehavior();
    }

    private void OnShellUnloaded(object? sender, EventArgs e)
    {
        if (observedWindow != null)
            observedWindow.PropertyChanged -= OnWindowPropertyChanged;

        observedWindow = null;
    }

    private void OnWindowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Window.Width))
            ApplyFlyoutBehavior();
    }

    private void OnShellSizeChanged(object? sender, EventArgs e) => ApplyFlyoutBehavior();

    private void ApplyFlyoutBehavior()
    {
        var availableWidth = observedWindow?.Width > 0 ? observedWindow.Width : Width;
        var desiredBehavior = DeviceInfo.Current.Idiom != DeviceIdiom.Phone && availableWidth >= 1360
            ? FlyoutBehavior.Locked
            : FlyoutBehavior.Flyout;

        if (FlyoutBehavior != desiredBehavior)
            FlyoutBehavior = desiredBehavior;
    }

    /// <summary>
    /// Baut die selbstgebaute Flyout-Liste (Shell.FlyoutContent) aus den sichtbaren
    /// Shell-Roots plus der "Immobilie hinzufuegen"-Aktion auf. Kein ItemTemplate/
    /// MenuItemTemplate mehr: Der Android-Flyout-Adapter hat recycelte Zeilen falsch
    /// verdrahtet, sodass ein Tap die Navigation eines anderen Eintrags ausloesen konnte.
    /// </summary>
    void BuildFlyoutEntries()
    {
        foreach (var item in Items)
        {
            if (item.CurrentItem?.CurrentItem is not ShellContent content)
                continue;

            if (!item.FlyoutItemIsVisible || !content.FlyoutItemIsVisible)
                continue;

            flyoutEntries.Add(new FlyoutMenuEntry
            {
                Title = content.Title,
                Icon = content.Icon,
                Route = content.Route
            });
        }

        // Aktion statt Ziel: pusht den Inserat-Wizard (mit Zurueck-Pfeil zum Abbrechen)
        flyoutEntries.Add(new FlyoutMenuEntry
        {
            Title = Loc.AddPropertyTitle,
            Icon = "icon_add.png",
            Route = "PropertyWizard",
            IsRoot = false
        });

        BindableLayout.SetItemsSource(FlyoutMenuList, flyoutEntries);
    }

    /// <summary>
    /// Tap auf einen Flyout-Eintrag: Roots absolut ansteuern (Wechsel des Shell-Roots),
    /// Aktionen relativ pushen (Zurueck-Pfeil zum Abbrechen)
    /// </summary>
    async void OnFlyoutEntryTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not FlyoutMenuEntry entry)
            return;

        FlyoutIsPresented = false;
        await GoToAsync(entry.IsRoot ? $"//{entry.Route}" : entry.Route);
    }

    /// <summary>
    /// Markiert den aktiven Root-Eintrag in der Flyout-Liste (erstes Segment der Route,
    /// damit die Markierung auch auf gepushten Detailseiten erhalten bleibt)
    /// </summary>
    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        var segments = CurrentState?.Location?.OriginalString.Trim('/').Split('/');
        var currentRoot = segments is { Length: > 0 } ? segments[0] : null;

        foreach (var entry in flyoutEntries)
            entry.IsSelected = entry.IsRoot && entry.Route == currentRoot;
    }

    /// <summary>
    /// Footer-Link Impressum: Flyout schliessen und Seite auf den Navigationsstack pushen
    /// </summary>
    private async void OnImprintTapped(object? sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("Imprint");
    }

    /// <summary>
    /// Footer-Link Datenschutz: Flyout schliessen und Seite auf den Navigationsstack pushen
    /// </summary>
    private async void OnPrivacyPolicyTapped(object? sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("PrivacyPolicy");
    }
}
