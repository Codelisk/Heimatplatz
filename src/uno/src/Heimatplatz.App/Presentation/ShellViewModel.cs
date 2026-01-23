using Heimatplatz.Features.Debug.Presentation;

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
#if DEBUG
        // Im Debug-Modus zur DebugStartPage navigieren für schnelles Testen
        await _navigator.NavigateViewModelAsync<DebugStartViewModel>(this);
#else
        // In Release zur MainPage navigieren
        await _navigator.NavigateViewModelAsync<MainViewModel>(this);
#endif
    }
}
