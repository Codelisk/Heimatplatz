namespace Heimatplatz.Features.AppUpdate.Contracts.Models;

/// <summary>
/// Options for configuring in-app updates behavior.
/// </summary>
public class AppUpdateOptions
{
    /// <summary>
    /// Default request code for update flow.
    /// </summary>
    public const int DefaultRequestCode = 4711;

    /// <summary>
    /// The minimum update priority (0-5) required to trigger an immediate update.
    /// Updates with priority below this threshold will use flexible update.
    /// Default is 4.
    /// </summary>
    public int ImmediateUpdatePriority { get; set; } = 4;

    /// <summary>
    /// Request code used to identify the update flow result.
    /// </summary>
    public int RequestCode { get; set; } = DefaultRequestCode;

    /// <summary>
    /// Whether to allow deletion of asset packs during update.
    /// Default is false to prevent data loss.
    /// </summary>
    public bool AllowAssetPackDeletion { get; set; }
}
