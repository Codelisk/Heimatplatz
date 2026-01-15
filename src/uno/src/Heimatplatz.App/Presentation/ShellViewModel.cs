using Heimatplatz.Features.Properties.Presentation;

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
        await _navigator.NavigateViewModelAsync<HomeViewModel>(this);
    }
}
