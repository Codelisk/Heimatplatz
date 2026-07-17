using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    readonly List<FlyoutMenuEntry> flyoutEntries = [];

    public AppShell()
    {
        InitializeComponent();

#if DEBUG
        // Debug-Werkzeuge (z.B. API-Umschalter) nur in Entwicklungs-Builds im Flyout
        Items.Add(new ShellContent
        {
            Title = "Debug",
            Icon = "icon_bug.png",
            Route = "Debug",
            ContentTemplate = new DataTemplate(typeof(Features.Debug.Presentation.DebugPage))
        });
#endif

        BuildFlyoutEntries();

        VersionLabel.Text = $"Heimatplatz · Version {AppInfo.Current.VersionString}";
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
            Title = "Immobilie hinzufügen",
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
