using Heimatplatz.Maui.Core.Build;
using Heimatplatz.Maui.Core.Handlers;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Localization;
using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    readonly List<FlyoutMenuEntry> flyoutEntries = [];
    Window? observedWindow;

    /// <summary>
    /// Prozess-lokales Gedaechtnis der letzten Shell-Route. Ueberlebt Android-
    /// Activity-Recreates (z.B. System-Themewechsel), bewusst ohne Persistenz:
    /// ein Kaltstart beginnt weiterhin auf der Startseite.
    /// </summary>
    internal static string? LastKnownLocation { get; private set; }

    private readonly IApiEndpointService apiEndpoints;

    public AppShellStringsLocalized Loc { get; }

    public AppShell(AppShellStringsLocalized loc, IApiEndpointService apiEndpoints)
    {
        Loc = loc;
        this.apiEndpoints = apiEndpoints;
        // Shell ist ihr eigener BindingContext: XAML bindet Titel/Links auf Loc.*
        BindingContext = this;
        InitializeComponent();

        // Debug-Werkzeuge (API-Umschalter, Test-Anmeldungen) in Entwicklungs- UND
        // internen Test-Builds (Play-Test-Tracks, TestFlight) - in der Store-Version
        // fehlt der Eintrag und damit jeder Weg auf die Seite.
        if (AppChannels.AreDeveloperToolsEnabled)
        {
            Items.Add(new ShellContent
            {
                Title = Loc.DebugTitle,
                Icon = "icon_bug.png",
                Route = "Debug",
                ContentTemplate = new DataTemplate(typeof(Features.Debug.Presentation.DebugPage))
            });
        }

        BuildFlyoutEntries();

        VersionLabel.Text = Loc.VersionFormat(AppInfo.Current.VersionString);
        UpdateEnvironmentBadge();

        // Das Abo auf Endpunktwechsel haengt an Loaded/Unloaded (siehe OnShellLoaded)

        // Tablet bleibt beim ueberlagernden Drawer. Erst auf wirklich breiten
        // Desktop-Fenstern wird die Navigation dauerhaft sichtbar; Phones bleiben
        // unabhaengig von ihrer Ausrichtung beim bisherigen Flyout.
        SizeChanged += OnShellSizeChanged;
        Loaded += OnShellLoaded;
        Unloaded += OnShellUnloaded;
    }

    /// <summary>
    /// Beschriftet die Umgebungs-Pille der Flyout-Fusszeile mit Kanal und aktiver API
    /// (z.B. "TestFlight · Test-API"). In Store-Builds bleibt sie unsichtbar - dort gibt
    /// es weder Umschalter noch etwas zu unterscheiden.
    /// </summary>
    private void UpdateEnvironmentBadge()
    {
        // EnvironmentBadge existiert erst nach InitializeComponent - OnPropertyChanged
        // feuert aber schon davor (BindingContext-Zuweisung)
        if (!AppChannels.AreDeveloperToolsEnabled || EnvironmentBadge is null)
            return;

        var activeEndpoint = ApiEndpoints.GetKindForUrl(apiEndpoints.CurrentUrl);
        EnvironmentBadgeLabel.Text = $"{AppChannels.DisplayName} · {ApiEndpoints.GetDisplayName(activeEndpoint)}";
        EnvironmentBadge.IsVisible = true;
    }

    /// <summary>Endpunktwechsel auf der Debug-Seite: Pille sofort nachziehen</summary>
    private void OnApiEndpointChanged(object? sender, EventArgs e) =>
        Dispatcher.Dispatch(UpdateEnvironmentBadge);

    /// <summary>
    /// Zweiter Aufhaenger fuer die Pille: das Oeffnen des Flyouts. Deckt Zustaende ab,
    /// die ohne Wechsel-Ereignis entstehen (z.B. Env-Override beim Start).
    /// </summary>
    protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
            UpdateEnvironmentBadge();
    }

    private void OnShellLoaded(object? sender, EventArgs e)
    {
        WindowsShellBackButton.Apply(this);

        // Abo nach einem Unloaded/Loaded-Zyklus wiederherstellen (idempotent)
        if (AppChannels.AreDeveloperToolsEnabled)
        {
            apiEndpoints.EndpointChanged -= OnApiEndpointChanged;
            apiEndpoints.EndpointChanged += OnApiEndpointChanged;
            UpdateEnvironmentBadge();
        }

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

        // Der Endpunkt-Service ist ein Singleton und wuerde die Shell sonst ueber ein
        // Android-Activity-Recreate hinaus am Leben halten
        if (AppChannels.AreDeveloperToolsEnabled)
            apiEndpoints.EndpointChanged -= OnApiEndpointChanged;
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

        RestoreLockedFlyoutPresentation();
    }

    /// <summary>
    /// Ein geschlossenes Flyout bleibt unter WinUI im "Locked"-Modus als schmale
    /// Icon-Leiste (~48px) stehen. Locked ist dauerhaft sichtbar und muss deshalb
    /// explizit praesentiert werden - auch nach jeder Navigation, weil Shell das
    /// Flyout danach grundsaetzlich schliesst.
    /// </summary>
    private void RestoreLockedFlyoutPresentation()
    {
        if (FlyoutBehavior != FlyoutBehavior.Locked || FlyoutIsPresented)
            return;

        // Seiten wie Impressum/Datenschutz deaktivieren das Flyout fuer sich
        // selbst - dort darf die Praesentation nicht erzwungen werden.
        if (CurrentPage is Page page &&
            page.IsSet(Shell.FlyoutBehaviorProperty) &&
            Shell.GetFlyoutBehavior(page) == FlyoutBehavior.Disabled)
        {
            return;
        }

        FlyoutIsPresented = true;
    }

    private void CloseTransientFlyout()
    {
        if (FlyoutBehavior != FlyoutBehavior.Locked)
            FlyoutIsPresented = false;
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

        CloseTransientFlyout();
        await GoToAsync(entry.IsRoot ? $"//{entry.Route}" : entry.Route);
    }

    /// <summary>
    /// Markiert den aktiven Root-Eintrag in der Flyout-Liste (erstes Segment der Route,
    /// damit die Markierung auch auf gepushten Detailseiten erhalten bleibt)
    /// </summary>
    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Shell schliesst das Flyout nach jeder Navigation auch im Locked-Modus -
        // die fixierte Desktop-Seitenleiste sofort wieder oeffnen (HP-MAUI-001).
        RestoreLockedFlyoutPresentation();

        LastKnownLocation = CurrentState?.Location?.OriginalString;

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
        CloseTransientFlyout();
        await GoToAsync("Imprint");
    }

    /// <summary>
    /// Footer-Link Datenschutz: Flyout schliessen und Seite auf den Navigationsstack pushen
    /// </summary>
    private async void OnPrivacyPolicyTapped(object? sender, TappedEventArgs e)
    {
        CloseTransientFlyout();
        await GoToAsync("PrivacyPolicy");
    }
}
