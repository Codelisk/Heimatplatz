using Heimatplatz.App.Controls;
using Heimatplatz.Features.Properties.Presentation;
#if DEBUG
using Heimatplatz.Features.Debug.Presentation;
#endif

namespace Heimatplatz.App.Presentation;

public class ShellViewModel
{
    private readonly INavigator _navigator;

    public AppHeaderViewModel AppHeader { get; }

    public ShellViewModel(
        INavigator navigator,
        AppHeaderViewModel appHeaderViewModel)
    {
        _navigator = navigator;
        AppHeader = appHeaderViewModel;

        _ = Start();
    }

    public async Task Start()
    {
#if DEBUG
        await _navigator.NavigateViewModelAsync<DebugStartViewModel>(this);
#else
        await _navigator.NavigateViewModelAsync<HomeViewModel>(this);
#endif
    }
}
