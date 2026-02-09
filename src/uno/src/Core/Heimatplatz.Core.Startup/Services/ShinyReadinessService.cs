using Heimatplatz;

namespace Heimatplatz.Core.Startup.Services;

/// <summary>
/// Default implementation of IShinyReadinessService.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class ShinyReadinessService : IShinyReadinessService
{
    private readonly TaskCompletionSource<bool> _readyTcs = new();

    public Task WaitForReadyAsync() => _readyTcs.Task;

    public void SignalReady() => _readyTcs.TrySetResult(true);
}
