namespace Heimatplatz.Features.AppUpdate.Contracts.Models;

/// <summary>
/// Information about an available app update.
/// </summary>
/// <param name="IsUpdateAvailable">Whether an update is available.</param>
/// <param name="IsImmediateUpdateAllowed">Whether immediate (blocking) updates are allowed.</param>
/// <param name="IsFlexibleUpdateAllowed">Whether flexible (background) updates are allowed.</param>
/// <param name="AvailableVersionCode">The version code of the available update.</param>
/// <param name="UpdatePriority">The update priority (0-5) set in Google Play Console.</param>
public record AppUpdateInfo(
    bool IsUpdateAvailable,
    bool IsImmediateUpdateAllowed,
    bool IsFlexibleUpdateAllowed,
    int AvailableVersionCode,
    int UpdatePriority);
