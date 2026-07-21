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
        var shell = new AppShell(_services.GetRequiredService<AppShellStringsLocalized>());
        ScreenshotMode.TryApply(shell, _services);
        var window = new Window(shell);

        // Session-Restore + Push-Init (fire-and-forget, blockiert den Start nicht)
        _ = _startup.StartAsync();

        // Nach laengerer Hintergrund-Zeit koennen Immobilien veraltet sein -
        // beim Zurueckkehren sofort einen Delta-Sync anstossen
        window.Resumed += (_, _) => _startup.OnAppResumed();

        // Beim Initialize im App-Konstruktor existiert das native Window noch nicht -
        // erzwungenes Hell/Dunkel hier nachziehen (Android: Systemleisten,
        // iOS: OverrideUserInterfaceStyle fuer System-Flaechen wie Pull-to-Refresh)
        window.Created += (_, _) => _theme.Apply();

        return window;
    }
}
