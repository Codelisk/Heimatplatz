#if DEBUG
using Heimatplatz.Features.Debug.Presentation;
#endif

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
        await _navigator.NavigateViewModelAsync<DebugStartViewModel>(this);
#else
        await _navigator.NavigateViewModelAsync<MainViewModel>(this);
#endif
    }
}
