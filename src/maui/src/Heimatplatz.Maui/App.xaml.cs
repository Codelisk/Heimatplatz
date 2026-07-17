using Heimatplatz.Maui.Core.Screenshots;
using Heimatplatz.Maui.Services;

namespace Heimatplatz.Maui;

public partial class App : Application
{
    private readonly AppStartupService _startup;
    private readonly IServiceProvider _services;

    public App(AppStartupService startup, IServiceProvider services)
    {
        InitializeComponent();
        _startup = startup;
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        ScreenshotMode.TryApply(shell, _services);
        var window = new Window(shell);

        // Session-Restore + Push-Init (fire-and-forget, blockiert den Start nicht)
        _ = _startup.StartAsync();

        // Nach laengerer Hintergrund-Zeit koennen Immobilien veraltet sein -
        // beim Zurueckkehren sofort einen Delta-Sync anstossen
        window.Resumed += (_, _) => _startup.OnAppResumed();

        return window;
    }
}
