using Heimatplatz.Maui.Core.Screenshots;
using Heimatplatz.Maui.Core.Theming;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Maui;

public partial class App : Application
{
    private readonly AppStartupService _startup;
    private readonly IServiceProvider _services;
    private readonly IThemeService _theme;

    public App(AppStartupService startup, IServiceProvider services, IThemeService theme)
    {
        InitializeComponent();
        _startup = startup;
        _services = services;
        _theme = theme;

        // Gespeicherten Design-Modus anwenden, bevor die erste Seite rendert
        _theme.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Auf iOS existiert das native UIWindow in diesem Moment bereits im
        // ActivationState. Theme vor dem Shell-Aufbau anwenden, damit native
        // Scroll-/Refresh-Flaechen nicht zuerst im Geraete-Theme entstehen.
        _theme.PrepareWindow(activationState);

        // Vor dem Shell-Aufbau sichern: die Startnavigation der neuen Shell
        // ueberschreibt AppShell.LastKnownLocation sofort mit "//MainPage".
        var restoreLocation = AppShell.LastKnownLocation;

        var shell = new AppShell(
            _services.GetRequiredService<AppShellStringsLocalized>(),
            _services.GetRequiredService<Features.Debug.Services.IApiEndpointService>());
        ScreenshotMode.TryApply(shell, _services);
        var window = new Window(shell);

        // Session-Restore + Push-Init (fire-and-forget, blockiert den Start nicht)
        var startupTask = _startup.StartAsync();

        // Warm-Recreate (Android: System-Themewechsel erzeugt die Activity neu):
        // die zuletzt aktive Route wiederherstellen statt auf die Startseite
        // zurueckzufallen. Erst nach dem Session-Restore, damit auth-abhaengige
        // Seiten nicht faelschlich zum Login umleiten.
        if (!string.IsNullOrEmpty(restoreLocation) &&
            restoreLocation.TrimEnd('/') != "//MainPage")
        {
            _ = RestoreNavigationAsync(shell, startupTask, restoreLocation);
        }

        // Nach laengerer Hintergrund-Zeit koennen Immobilien veraltet sein -
        // beim Zurueckkehren sofort einen Delta-Sync anstossen
        window.Resumed += (_, _) => _startup.OnAppResumed();

        // Beim Initialize im App-Konstruktor existiert das native Window noch nicht -
        // erzwungenes Hell/Dunkel hier nachziehen (Android: Systemleisten,
        // iOS: OverrideUserInterfaceStyle fuer System-Flaechen wie Pull-to-Refresh).
        // Activated zusaetzlich, weil bei Created auf iOS das UIWindow noch nicht
        // zuverlaessig an der Scene haengt - Apply ist idempotent.
        window.Created += (_, _) => _theme.Apply();
        window.Activated += (_, _) => _theme.Apply();

        return window;
    }

    private static async Task RestoreNavigationAsync(AppShell shell, Task startupTask, string location)
    {
        try
        {
            await startupTask;
            await WaitForLoadedAsync(shell);
            await shell.Dispatcher.DispatchAsync(() => shell.GoToAsync(location));
        }
        catch
        {
            // Route nicht mehr aufloesbar (z.B. nach Logout) - der sichere
            // Fallback ist der normale Start auf der Root-Seite.
        }
    }

    private static Task WaitForLoadedAsync(AppShell shell)
    {
        if (shell.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnLoaded(object? sender, EventArgs e)
        {
            shell.Loaded -= OnLoaded;
            tcs.TrySetResult();
        }

        shell.Loaded += OnLoaded;
        return tcs.Task;
    }
}
