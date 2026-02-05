#if __ANDROID__
using Android.App;
using Android.Gms.Tasks;
using Microsoft.Extensions.Logging;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;
using PlayCoreAppUpdateInfo = Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateInfo;
using PlayCoreAppUpdateOptions = Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateOptions;
using AndroidTask = Android.Gms.Tasks.Task;
using Task = System.Threading.Tasks.Task;

namespace Heimatplatz.Features.AppUpdate.Platforms.Android;

/// <summary>
/// Android implementation of <see cref="Contracts.IAppUpdateService"/> using Google Play Core.
/// </summary>
internal sealed class AndroidAppUpdateService : Java.Lang.Object, Contracts.IAppUpdateService, IInstallStateUpdatedListener
{
    private readonly ILogger<AndroidAppUpdateService> _logger;
    private readonly IAppUpdateManager _appUpdateManager;
    private TaskCompletionSource<Contracts.Models.UpdateResult>? _updateFlowTcs;

    public Contracts.Models.AppUpdateOptions Options { get; set; } = new();

    public event EventHandler<Contracts.Models.UpdateDownloadProgress>? DownloadProgressChanged;
    public event EventHandler? UpdateDownloaded;
    public event EventHandler<Contracts.Models.UpdateResult>? UpdateCompleted;

    public AndroidAppUpdateService(ILogger<AndroidAppUpdateService> logger)
    {
        _logger = logger;

        var activity = GetCurrentActivity();
        _appUpdateManager = AppUpdateManagerFactory.Create(activity);
        _appUpdateManager.RegisterListener(this);

        _logger.LogDebug("AndroidAppUpdateService initialized");
    }

    public async Task<Contracts.Models.AppUpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var appUpdateInfoTask = _appUpdateManager.GetAppUpdateInfo();
            var info = await appUpdateInfoTask.AsTask<PlayCoreAppUpdateInfo>();

            var updateAvailability = info.UpdateAvailability();
            var isUpdateAvailable = updateAvailability == UpdateAvailability.UpdateAvailable ||
                                    updateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;

            _logger.LogInformation(
                "Update check: Available={IsAvailable}, VersionCode={VersionCode}, Priority={Priority}",
                isUpdateAvailable,
                info.AvailableVersionCode(),
                info.UpdatePriority());

            return new Contracts.Models.AppUpdateInfo(
                IsUpdateAvailable: isUpdateAvailable,
                IsImmediateUpdateAllowed: info.IsUpdateTypeAllowed(AppUpdateType.Immediate),
                IsFlexibleUpdateAllowed: info.IsUpdateTypeAllowed(AppUpdateType.Flexible),
                AvailableVersionCode: info.AvailableVersionCode(),
                UpdatePriority: info.UpdatePriority());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return null;
        }
    }

    public async Task<bool> StartImmediateUpdateAsync()
    {
        try
        {
            var activity = GetCurrentActivity();
            var appUpdateInfoTask = _appUpdateManager.GetAppUpdateInfo();
            var info = await appUpdateInfoTask.AsTask<PlayCoreAppUpdateInfo>();

            if (!info.IsUpdateTypeAllowed(AppUpdateType.Immediate))
            {
                _logger.LogWarning("Immediate update not allowed");
                return false;
            }

            _updateFlowTcs = new TaskCompletionSource<Contracts.Models.UpdateResult>();

            var result = _appUpdateManager.StartUpdateFlowForResult(
                info,
                activity,
                PlayCoreAppUpdateOptions.NewBuilder(AppUpdateType.Immediate)
                    .SetAllowAssetPackDeletion(Options.AllowAssetPackDeletion)
                    .Build(),
                Options.RequestCode);

            _logger.LogInformation("Started immediate update flow");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start immediate update");
            return false;
        }
    }

    public async Task<bool> StartFlexibleUpdateAsync()
    {
        try
        {
            var activity = GetCurrentActivity();
            var appUpdateInfoTask = _appUpdateManager.GetAppUpdateInfo();
            var info = await appUpdateInfoTask.AsTask<PlayCoreAppUpdateInfo>();

            if (!info.IsUpdateTypeAllowed(AppUpdateType.Flexible))
            {
                _logger.LogWarning("Flexible update not allowed");
                return false;
            }

            _updateFlowTcs = new TaskCompletionSource<Contracts.Models.UpdateResult>();

            var result = _appUpdateManager.StartUpdateFlowForResult(
                info,
                activity,
                PlayCoreAppUpdateOptions.NewBuilder(AppUpdateType.Flexible)
                    .SetAllowAssetPackDeletion(Options.AllowAssetPackDeletion)
                    .Build(),
                Options.RequestCode);

            _logger.LogInformation("Started flexible update flow");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start flexible update");
            return false;
        }
    }

    public Task CompleteUpdateAsync()
    {
        _logger.LogInformation("Completing update and restarting app");
        _appUpdateManager.CompleteUpdate();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called by the Play Core library when install state changes.
    /// </summary>
    public void OnStateUpdate(Java.Lang.Object state)
    {
        if (state is not InstallState installState)
        {
            return;
        }

        var status = installState.InstallStatus();
        _logger.LogDebug("Install state update: {Status}", status);

        switch (status)
        {
            case InstallStatus.Downloading:
                var progress = new Contracts.Models.UpdateDownloadProgress(
                    installState.BytesDownloaded(),
                    installState.TotalBytesToDownload());
                DownloadProgressChanged?.Invoke(this, progress);
                break;

            case InstallStatus.Downloaded:
                _logger.LogInformation("Update downloaded, ready to install");
                UpdateDownloaded?.Invoke(this, EventArgs.Empty);
                break;

            case InstallStatus.Installed:
                _logger.LogInformation("Update installed successfully");
                UpdateCompleted?.Invoke(this, Contracts.Models.UpdateResult.Success);
                _updateFlowTcs?.TrySetResult(Contracts.Models.UpdateResult.Success);
                break;

            case InstallStatus.Failed:
                _logger.LogError("Update installation failed");
                UpdateCompleted?.Invoke(this, Contracts.Models.UpdateResult.Failed);
                _updateFlowTcs?.TrySetResult(Contracts.Models.UpdateResult.Failed);
                break;

            case InstallStatus.Canceled:
                _logger.LogInformation("Update cancelled by user");
                UpdateCompleted?.Invoke(this, Contracts.Models.UpdateResult.Cancelled);
                _updateFlowTcs?.TrySetResult(Contracts.Models.UpdateResult.Cancelled);
                break;
        }
    }

    /// <summary>
    /// Handles activity result from the update flow.
    /// Call this from MainActivity.OnActivityResult.
    /// </summary>
    public void HandleActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode != Options.RequestCode)
        {
            return;
        }

        var result = resultCode switch
        {
            Result.Ok => Contracts.Models.UpdateResult.Success,
            Result.Canceled => Contracts.Models.UpdateResult.Cancelled,
            _ => Contracts.Models.UpdateResult.Failed
        };

        _logger.LogInformation("Update activity result: {Result}", result);
        UpdateCompleted?.Invoke(this, result);
        _updateFlowTcs?.TrySetResult(result);
    }

    private static Activity GetCurrentActivity()
    {
        // Use Uno Platform's context helper instead of MAUI
        var context = Uno.UI.ContextHelper.Current;
        if (context is Activity activity)
        {
            return activity;
        }

        throw new InvalidOperationException("No current activity available");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _appUpdateManager.UnregisterListener(this);
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Extension methods for converting Google Play Tasks to .NET Tasks.
/// </summary>
internal static class TaskExtensions
{
    public static Task<T> AsTask<T>(this AndroidTask task) where T : Java.Lang.Object
    {
        var tcs = new TaskCompletionSource<T>();

        task.AddOnSuccessListener(new SuccessListener<T>(tcs));
        task.AddOnFailureListener(new FailureListener<T>(tcs));

        return tcs.Task;
    }

    private sealed class SuccessListener<T>(TaskCompletionSource<T> tcs) : Java.Lang.Object, IOnSuccessListener
        where T : Java.Lang.Object
    {
        public void OnSuccess(Java.Lang.Object? result)
        {
            if (result is T typedResult)
            {
                tcs.TrySetResult(typedResult);
            }
            else
            {
                tcs.TrySetException(new InvalidCastException($"Expected {typeof(T)}, got {result?.GetType()}"));
            }
        }
    }

    private sealed class FailureListener<T>(TaskCompletionSource<T> tcs) : Java.Lang.Object, IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e)
        {
            tcs.TrySetException(new Exception(e.Message));
        }
    }
}
#endif
