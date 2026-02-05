using Heimatplatz.Features.AppUpdate.Contracts;
using Heimatplatz.Features.AppUpdate.Contracts.Models;

namespace Heimatplatz.Features.AppUpdate.Services;

/// <summary>
/// No-op implementation of <see cref="IAppUpdateService"/> for non-Android platforms.
/// </summary>
internal sealed class NoOpAppUpdateService : IAppUpdateService
{
    public AppUpdateOptions Options { get; set; } = new();

    public Task<AppUpdateInfo?> CheckForUpdateAsync() => Task.FromResult<AppUpdateInfo?>(null);

    public Task<bool> StartImmediateUpdateAsync() => Task.FromResult(false);

    public Task<bool> StartFlexibleUpdateAsync() => Task.FromResult(false);

    public Task CompleteUpdateAsync() => Task.CompletedTask;

#pragma warning disable CS0067 // Event is never used (expected for no-op implementation)
    public event EventHandler<UpdateDownloadProgress>? DownloadProgressChanged;
    public event EventHandler? UpdateDownloaded;
    public event EventHandler<UpdateResult>? UpdateCompleted;
#pragma warning restore CS0067
}
