namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service to manage user's property favorites and blocked status.
/// Caches the status locally and syncs with API.
/// </summary>
public interface IPropertyStatusService
{
    /// <summary>
    /// Event raised when favorites or blocked status changes
    /// </summary>
    event EventHandler? StatusChanged;

    /// <summary>
    /// Check if a property is favorited by the current user
    /// </summary>
    bool IsFavorite(Guid propertyId);

    /// <summary>
    /// Check if a property is blocked by the current user
    /// </summary>
    bool IsBlocked(Guid propertyId);

    /// <summary>
    /// Toggle favorite status for a property
    /// </summary>
    /// <returns>True if now favorited, false if unfavorited</returns>
    Task<bool> ToggleFavoriteAsync(Guid propertyId);

    /// <summary>
    /// Toggle blocked status for a property
    /// </summary>
    /// <returns>True if now blocked, false if unblocked</returns>
    Task<bool> ToggleBlockedAsync(Guid propertyId);

    /// <summary>
    /// Load/refresh the user's favorites and blocked lists from API
    /// </summary>
    Task RefreshStatusAsync();

    /// <summary>
    /// Ensure status is loaded (calls RefreshStatusAsync if not loaded yet)
    /// </summary>
    Task EnsureLoadedAsync();

    /// <summary>
    /// Clear cached status (e.g., on logout)
    /// </summary>
    void ClearCache();
}
