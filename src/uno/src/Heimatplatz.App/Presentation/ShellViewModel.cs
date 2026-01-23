namespace Heimatplatz.App.Presentation;

public class ShellViewModel
{
    private readonly INavigator _navigator;

    public ShellViewModel(INavigator navigator)
    {
        _navigator = navigator;

        _ = Start();
    }

    public async Task Start()
    {
        // Navigation zur Default-Route (Main) - IsDefault in RouteMap bestimmt die Startseite
        await _navigator.NavigateViewModelAsync<MainViewModel>(this);
    }
}
