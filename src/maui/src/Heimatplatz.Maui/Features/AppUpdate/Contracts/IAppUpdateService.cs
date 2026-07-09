using Heimatplatz.Features.AppUpdate.Contracts.Models;

namespace Heimatplatz.Features.AppUpdate.Contracts;

/// <summary>
/// Service for managing in-app updates on Android.
/// On non-Android platforms, this service is a no-op.
/// </summary>
public interface IAppUpdateService
{
    /// <summary>
    /// Gets or sets the options for configuring update behavior.
    /// </summary>
    AppUpdateOptions Options { get; set; }

    /// <summary>
    /// Checks if an update is available.
    /// </summary>
    /// <returns>Update information, or null if check failed or not supported.</returns>
    Task<AppUpdateInfo?> CheckForUpdateAsync();

    /// <summary>
    /// Starts an immediate (blocking) update flow.
    /// The app will be blocked until the update is installed.
    /// </summary>
    /// <returns>True if the update flow was started successfully.</returns>
    Task<bool> StartImmediateUpdateAsync();

    /// <summary>
    /// Starts a flexible (background) update flow.
    /// The update downloads in the background while the user continues using the app.
    /// </summary>
    /// <returns>True if the update flow was started successfully.</returns>
    Task<bool> StartFlexibleUpdateAsync();

    /// <summary>
    /// Completes a pending flexible update by installing it and restarting the app.
    /// Call this after a flexible update has been downloaded.
    /// </summary>
    Task CompleteUpdateAsync();

    /// <summary>
    /// Event raised when download progress changes during a flexible update.
    /// </summary>
    event EventHandler<UpdateDownloadProgress>? DownloadProgressChanged;

    /// <summary>
    /// Event raised when a flexible update download completes and is ready to install.
    /// </summary>
    event EventHandler? UpdateDownloaded;

    /// <summary>
    /// Event raised when an update flow completes.
    /// </summary>
    event EventHandler<UpdateResult>? UpdateCompleted;
}
