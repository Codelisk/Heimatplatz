using Heimatplatz.Maui.Services;

namespace Heimatplatz.Maui;

public partial class App : Application
{
    private readonly AppStartupService _startup;

    public App(AppStartupService startup)
    {
        InitializeComponent();
        _startup = startup;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Session-Restore + Push-Init (fire-and-forget, blockiert den Start nicht)
        _ = _startup.StartAsync();

        return window;
    }
}
