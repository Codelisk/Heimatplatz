namespace Heimatplatz.Features.AppUpdate.Contracts.Models;

/// <summary>
/// Result of an update flow.
/// </summary>
public enum UpdateResult
{
    /// <summary>
    /// Update was successful.
    /// </summary>
    Success,

    /// <summary>
    /// User cancelled the update.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Update failed.
    /// </summary>
    Failed
}
